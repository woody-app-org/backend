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

public class PreLaunchSignupIntegrationTests
{
    [Fact]
    public async Task Signup_StoresIpHashNotPlainIp()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "Ana",
                socialNetwork = "instagram",
                socialUsername = "ana_unique_1",
                acceptedContact = true,
                website = (string?)null
            });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        var row = await db.PreLaunchSignups.SingleAsync(s => s.NormalizedSocialUsername == "ana_unique_1");
        Assert.NotNull(row.IpHash);
        Assert.Matches("^[a-f0-9]{64}$", row.IpHash!);
        Assert.DoesNotContain("127.0.0.1", row.IpHash);
        Assert.DoesNotContain("::1", row.IpHash);
    }

    [Fact]
    public async Task Signup_Duplicate_ReturnsOkIdempotent_DoesNotInsertTwice()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();
        var body = new
        {
            name = "Ana",
            socialNetwork = "instagram",
            socialUsername = "@Maria",
            acceptedContact = true,
            website = (string?)null
        };

        var first = await client.PostAsJsonAsync("/api/prelaunch/signups", body);
        var second = await client.PostAsJsonAsync("/api/prelaunch/signups", body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        var count = await db.PreLaunchSignups.CountAsync(s => s.NormalizedSocialUsername == "maria");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Signup_SecondCaseVariantSameNormalized_ReturnsOkSingleRow()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "B",
                socialNetwork = "Instagram",
                socialUsername = "Maria",
                acceptedContact = true
            });

        await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "C",
                socialNetwork = "instagram",
                socialUsername = "@maria",
                acceptedContact = true
            });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        Assert.Equal(1, await db.PreLaunchSignups.CountAsync(s => s.NormalizedSocialUsername == "maria"));
    }

    [Fact]
    public async Task Signup_UrlInstagram_NormalizesHandle()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "D",
                socialNetwork = "instagram",
                socialUsername = "https://instagram.com/maria",
                acceptedContact = true
            });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        Assert.Equal(1, await db.PreLaunchSignups.CountAsync(s => s.NormalizedSocialUsername == "maria"));
    }

    [Fact]
    public async Task Signup_SameUsernameDifferentNetworks_Allowed()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        var a = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "E1",
                socialNetwork = "instagram",
                socialUsername = "duphandle",
                acceptedContact = true
            });
        var b = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "E2",
                socialNetwork = "facebook",
                socialUsername = "duphandle",
                acceptedContact = true
            });

        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(HttpStatusCode.OK, b.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        Assert.Equal(2, await db.PreLaunchSignups.CountAsync(s => s.NormalizedSocialUsername == "duphandle"));
    }

    [Fact]
    public async Task Signup_AcceptedContactFalse_Returns400()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "F",
                socialNetwork = "instagram",
                socialUsername = "fuser",
                acceptedContact = false
            });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Signup_HoneypotFilled_ReturnsOk_DoesNotPersist()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        var before = await CountAsync(factory);

        var res = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "Bot",
                socialNetwork = "instagram",
                socialUsername = "botuser",
                acceptedContact = true,
                website = "https://spam.example"
            });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var after = await CountAsync(factory);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Signup_InvalidNetwork_Returns400()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "G",
                socialNetwork = "myspace",
                socialUsername = "guser",
                acceptedContact = true
            });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Rede social inválida", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signup_FourthDistinctUserWithin24h_Returns429()
    {
        await using var factory = new PreLaunchApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var r = await client.PostAsJsonAsync(
                "/api/prelaunch/signups",
                new
                {
                    name = $"Cap{i}",
                    socialNetwork = "instagram",
                    socialUsername = $"capuser{i}",
                    acceptedContact = true
                });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var fourth = await client.PostAsJsonAsync(
            "/api/prelaunch/signups",
            new
            {
                name = "CapOver",
                socialNetwork = "instagram",
                socialUsername = "capuser_over",
                acceptedContact = true
            });

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
        var json = await fourth.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("PRELAUNCH_RATE_LIMITED", doc.RootElement.GetProperty("code").GetString());
    }

    private static async Task<int> CountAsync(PreLaunchApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        return await db.PreLaunchSignups.CountAsync();
    }

    private sealed class PreLaunchApiFactory : WebApplicationFactory<Program>
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
            ["AuthSecurity:LockoutMinutes"] = "15",
            ["PreLaunch:HashSecret"] = "test-prelaunch-hash-secret-32bytes!!"
        };
    }
}
