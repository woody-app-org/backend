using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Tests;

public class AuthEmailRateLimitTests
{
    [Fact]
    public async Task SendVerification_SecondRequestWithinCooldown_Returns429WithPayload()
    {
        await using var factory = new EmailRateLimitApiFactory();
        var client = factory.CreateClient();

        var body = new { email = "cooldown-test@example.com" };
        var first = await client.PostAsJsonAsync("/api/auth/send-verification", body);
        // Pode ser 200 ou erro de negócio; o segundo pedido imediato deve ser barrado pelo rate limiter.
        _ = first.StatusCode;

        var second = await client.PostAsJsonAsync("/api/auth/send-verification", body);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.True(second.Headers.RetryAfter?.Delta is { TotalSeconds: > 0 });

        var json = await second.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("EMAIL_RATE_LIMITED", root.GetProperty("code").GetString());
        Assert.True(root.TryGetProperty("retryAfterSeconds", out var ra) && ra.GetInt32() > 0);
    }

    [Fact]
    public async Task VerifyEmail_EleventhRequest_Returns429()
    {
        await using var factory = new EmailRateLimitApiFactory();
        var client = factory.CreateClient();

        var body = new { email = "verify-burst@example.com", code = "000000" };
        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
            last = await client.PostAsJsonAsync("/api/auth/verify-email", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        var json = await last.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("EMAIL_VERIFY_RATE_LIMITED", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task VerifyEmail_NineRequests_UnderLimit_Never429()
    {
        await using var factory = new EmailRateLimitApiFactory();
        var client = factory.CreateClient();

        var body = new { email = "nine-tries@example.com", code = "000000" };
        for (var i = 0; i < 9; i++)
        {
            var r = await client.PostAsJsonAsync("/api/auth/verify-email", body);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, r.StatusCode);
        }
    }

    [Fact]
    public async Task SendVerification_SecondSendStillUsesSendPolicy_AfterManyVerifies()
    {
        await using var factory = new EmailRateLimitApiFactory();
        var client = factory.CreateClient();

        var email = "mix-limits@example.com";
        var first = await client.PostAsJsonAsync("/api/auth/send-verification", new { email });
        _ = first.StatusCode;

        for (var i = 0; i < 9; i++)
            await client.PostAsJsonAsync("/api/auth/verify-email", new { email, code = "000000" });

        var secondSend = await client.PostAsJsonAsync("/api/auth/send-verification", new { email });
        Assert.Equal(HttpStatusCode.TooManyRequests, secondSend.StatusCode);
        var json = await secondSend.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("EMAIL_RATE_LIMITED", doc.RootElement.GetProperty("code").GetString());
    }

    private sealed class EmailRateLimitApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

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
            });
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
