using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Woody.Application.DTOs;
using Woody.Application.Interfaces.Email;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;

namespace Woody.Api.Tests;

public class PasswordResetEndpointTests
{
    [Fact]
    public async Task Request_ReturnsGenericResponse_ForExistingAndMissingEmail()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.SeedUserAsync();
        var client = factory.CreateClient();

        var existing = await client.PostAsJsonAsync("/api/auth/password-reset/request", new { email = PasswordResetApiFactory.Email });
        var missing = await client.PostAsJsonAsync("/api/auth/password-reset/request", new { email = "ghost@example.com" });

        existing.EnsureSuccessStatusCode();
        missing.EnsureSuccessStatusCode();

        var existingBody = await existing.Content.ReadFromJsonAsync<RequestPasswordResetResponseDTO>();
        var missingBody = await missing.Content.ReadFromJsonAsync<RequestPasswordResetResponseDTO>();

        Assert.Equal(PasswordResetService.GenericRequestMessage, existingBody!.Message);
        Assert.Equal(PasswordResetService.GenericRequestMessage, missingBody!.Message);
        Assert.Equal("be*****@example.com", existingBody.MaskedEmail);
        Assert.Equal("gh*****@example.com", missingBody.MaskedEmail);
    }

    [Fact]
    public async Task FullFlow_ChangesPassword_RevokesRefreshToken_AndAllowsLoginWithNewPassword()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.SeedUserAsync();
        var client = factory.CreateClient();

        var request = await client.PostAsJsonAsync("/api/auth/password-reset/request", new { email = PasswordResetApiFactory.Email });
        request.EnsureSuccessStatusCode();
        var code = factory.LastSentCode
                   ?? throw new InvalidOperationException("Expected password reset email to be sent.");

        var verify = await client.PostAsJsonAsync("/api/auth/password-reset/verify-code", new
        {
            email = PasswordResetApiFactory.Email,
            code
        });
        verify.EnsureSuccessStatusCode();
        var verified = await verify.Content.ReadFromJsonAsync<VerifyPasswordResetCodeResponseDTO>();

        var loginBefore = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = PasswordResetApiFactory.Email,
            Password = PasswordResetApiFactory.Password
        });
        loginBefore.EnsureSuccessStatusCode();
        var session = await loginBefore.Content.ReadFromJsonAsync<LoginResultDTO>();

        var confirm = await client.PostAsJsonAsync("/api/auth/password-reset/confirm", new
        {
            resetToken = verified!.ResetToken,
            newPassword = PasswordResetApiFactory.NewPassword,
            confirmPassword = PasswordResetApiFactory.NewPassword
        });
        confirm.EnsureSuccessStatusCode();

        var refreshAfterReset = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDTO
        {
            RefreshToken = session!.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterReset.StatusCode);

        var loginOld = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = PasswordResetApiFactory.Email,
            Password = PasswordResetApiFactory.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginOld.StatusCode);

        var loginNew = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = PasswordResetApiFactory.Email,
            Password = PasswordResetApiFactory.NewPassword
        });
        loginNew.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PasswordResetRequest_SecondRequestWithinCooldown_Returns429()
    {
        await using var factory = new PasswordResetApiFactory();
        await factory.SeedUserAsync();
        var client = factory.CreateClient();

        var body = new { email = PasswordResetApiFactory.Email };
        _ = await client.PostAsJsonAsync("/api/auth/password-reset/request", body);
        var second = await client.PostAsJsonAsync("/api/auth/password-reset/request", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    private sealed class PasswordResetApiFactory : WebApplicationFactory<Program>
    {
        public const string Email = "bea@example.com";
        public const string Password = "CorrectHorseBatteryStaple!1";
        public const string NewPassword = "BrandNewPassword1!";

        public string? LastSentCode { get; private set; }

        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly Mock<IEmailSender> _emailSender = new();

        public PasswordResetApiFactory()
        {
            _emailSender
                .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                .Callback<EmailMessage, CancellationToken>((message, _) =>
                {
                    var match = Regex.Match(message.HtmlBody, @"letter-spacing: 6px[^>]*>(\d{6})<");
                    if (match.Success)
                        LastSentCode = match.Groups[1].Value;
                })
                .Returns(Task.CompletedTask);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            foreach (var pair in BuildSettings())
            {
                if (pair.Value != null)
                    builder.UseSetting(pair.Key, pair.Value);
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<WoodyDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<WoodyDbContext>>();
                services.AddDbContext<WoodyDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                services.RemoveAll<IEmailSender>();
                services.AddSingleton(_emailSender.Object);
            });
        }

        public async Task SeedUserAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
            var hasher = new PasswordHasher();
            db.Users.Add(new User
            {
                Username = "bea",
                Email = Email,
                Password = hasher.HashPassword(Password),
                Role = "User",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        private static Dictionary<string, string?> BuildSettings() => new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=woody_tests;Username=postgres;Password=postgres",
            ["Jwt:Secret"] = "test-secret-that-is-at-least-32-chars",
            ["Jwt:Issuer"] = "Woody.Api.Tests",
            ["Jwt:Audience"] = "Woody.Api.Tests",
            ["Jwt:ExpirationMinutes"] = "15",
            ["Resend:ApiKey"] = "test-resend-key",
            ["Resend:FromEmail"] = "no-reply@example.com",
            ["EmailVerification:ExpirationMinutes"] = "10",
            ["EmailVerification:MaxAttempts"] = "5",
            ["AuthSecurity:MaxFailedLoginAttempts"] = "5",
            ["AuthSecurity:LockoutMinutes"] = "15"
        };
    }
}
