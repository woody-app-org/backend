using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Woody.Api.Tests;

public class ProductionStartupGuardTests
{
    [Fact]
    public void ProductionStartup_FailsWhenCorsOriginsIsMissing()
    {
        using var factory = new GuardFactory(new Dictionary<string, string?>
        {
            ["CORS_ORIGINS"] = ""
        });

        var ex = Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("CORS_ORIGINS", ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionStartup_FailsWhenStripeIsPartiallyConfiguredWithHttpReturnUrl()
    {
        using var factory = new GuardFactory(new Dictionary<string, string?>
        {
            ["CORS_ORIGINS"] = "https://app.example.com",
            ["Billing:Stripe:SecretKey"] = "sk_live_test",
            ["Billing:Stripe:WebhookSecret"] = "whsec_test",
            ["Billing:Stripe:PriceIds:ProMonthly"] = "price_live_test",
            ["Billing:Stripe:CheckoutSuccessUrl"] = "http://app.example.com/success",
            ["Billing:Stripe:CheckoutCancelUrl"] = "https://app.example.com/cancel",
            ["Billing:Stripe:CustomerPortalReturnUrl"] = "https://app.example.com/planos"
        });

        var ex = Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("CheckoutSuccessUrl", ex.ToString(), StringComparison.Ordinal);
    }

    private sealed class GuardFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?> _overrides;
        private readonly Dictionary<string, string?> _previousEnvironmentValues = new();

        public GuardFactory(IReadOnlyDictionary<string, string?> overrides)
        {
            _overrides = overrides;
            foreach (var item in overrides)
            {
                _previousEnvironmentValues[item.Key] = Environment.GetEnvironmentVariable(item.Key);
                Environment.SetEnvironmentVariable(item.Key, item.Value);
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=woody_tests;Username=postgres;Password=postgres",
                    ["Jwt:Secret"] = "production-secret-that-is-at-least-32-chars",
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

                foreach (var item in _overrides)
                    values[item.Key] = item.Value;

                config.AddInMemoryCollection(values);
            });
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var item in _previousEnvironmentValues)
                Environment.SetEnvironmentVariable(item.Key, item.Value);

            base.Dispose(disposing);
        }
    }
}
