using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woody.Application.DTOs;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;

namespace Woody.Api.Tests;

public class AuthEndpointTests
{
    [Fact]
    public async Task Login_ReturnsGenericError_ForInvalidCredentials()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Email = "missing@example.com",
            Password = "wrong"
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(LoginHandlerInvalidCredentialsMessage, body);
    }

    [Fact]
    public async Task LoginRefreshAndLogout_ManageRefreshTokenSession()
    {
        await using var factory = new AuthApiFactory();
        await factory.SeedUserAsync();
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            Username = AuthApiFactory.Username,
            Password = AuthApiFactory.Password
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResultDTO>();
        Assert.False(string.IsNullOrWhiteSpace(login?.Token));
        Assert.False(string.IsNullOrWhiteSpace(login?.RefreshToken));

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDTO
        {
            RefreshToken = login!.RefreshToken
        });
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResultDTO>();
        Assert.False(string.IsNullOrWhiteSpace(refreshed?.Token));
        Assert.False(string.IsNullOrWhiteSpace(refreshed?.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refreshed!.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Token);
        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequestDTO
        {
            RefreshToken = refreshed.RefreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDTO
        {
            RefreshToken = refreshed.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task AuthEndpoints_AreRateLimited()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? response = null;
        for (var i = 0; i < 6; i++)
        {
            response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
            {
                Email = $"missing-{i}@example.com",
                Password = "wrong"
            });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
    }

    private const string LoginHandlerInvalidCredentialsMessage = "Credenciais inválidas.";

    private sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        public const string Username = "bea";
        public const string Email = "bea@example.com";
        public const string Password = "CorrectHorseBatteryStaple!1";

        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            foreach (var pair in BuildAuthHostSettings())
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
            });
        }

        public async Task SeedUserAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
            var hasher = new PasswordHasher();
            var user = new User
            {
                Username = Username,
                Email = Email,
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Beatriz",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            db.UserSubscriptions.Add(new UserSubscription
            {
                UserId = user.Id,
                Plan = SubscriptionPlan.Free,
                Status = SubscriptionStatus.Active,
                BillingProvider = BillingProvider.None,
                PlanCode = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        private static Dictionary<string, string?> BuildAuthHostSettings() => new()
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
