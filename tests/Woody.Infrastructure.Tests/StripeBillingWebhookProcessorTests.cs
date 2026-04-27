using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Infrastructure.Billing.StripePayments;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class StripeBillingWebhookProcessorTests
{
    private const string WebhookSecret = "whsec_test_secret";

    [Fact]
    public async Task ProcessAsync_TreatsRepeatedWebhookAsDuplicateDelivery()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        await using var firstDb = database.CreateContext();
        await using var secondDb = database.CreateContext();
        var firstProcessor = CreateProcessor(firstDb);
        var secondProcessor = CreateProcessor(secondDb);
        var payload = """
            {
              "id": "evt_duplicate_invoice_paid",
              "object": "event",
              "api_version": "2025-01-27.acacia",
              "livemode": false,
              "pending_webhooks": 1,
              "request": { "id": null, "idempotency_key": null },
              "type": "invoice.paid",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "in_duplicate",
                  "object": "invoice"
                }
              }
            }
            """;
        var signature = CreateStripeSignatureHeader(payload);

        var first = await firstProcessor.ProcessAsync(payload, signature);
        var second = await secondProcessor.ProcessAsync(payload, signature);

        Assert.Equal(StripeWebhookProcessOutcome.Processed, first);
        Assert.Equal(StripeWebhookProcessOutcome.DuplicateDelivery, second);
        await using var assertDb = database.CreateContext();
        Assert.Equal(1, await assertDb.BillingWebhookReceipts.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_RollsBackReceiptWhenPayloadCannotBeApplied()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var processor = CreateProcessor(db);
        var payload = """
            {
              "id": "evt_invalid_checkout",
              "object": "event",
              "api_version": "2025-01-27.acacia",
              "livemode": false,
              "pending_webhooks": 1,
              "request": { "id": null, "idempotency_key": null },
              "type": "checkout.session.completed",
              "created": 1710000000,
              "data": {
                "object": {
                  "id": "cs_invalid",
                  "object": "checkout.session",
                  "mode": "subscription"
                }
              }
            }
            """;
        var signature = CreateStripeSignatureHeader(payload);

        var outcome = await processor.ProcessAsync(payload, signature);

        Assert.Equal(StripeWebhookProcessOutcome.InvalidPayload, outcome);
        await using var assertDb = database.CreateContext();
        Assert.Equal(0, await assertDb.BillingWebhookReceipts.CountAsync());
    }

    private static StripeBillingWebhookProcessor CreateProcessor(WoodyDbContext db)
    {
        var options = Options.Create(new BillingOptions
        {
            Stripe = new StripeBillingOptions
            {
                WebhookSecret = WebhookSecret
            }
        });

        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new StripeBillingWebhookProcessor(
            options,
            new BillingWebhookReceiptRepository(db),
            subscriptions.Object,
            new Mock<ICommunitySubscriptionRepository>().Object,
            new Mock<IBillingSubscriptionGateway>().Object,
            db,
            NullLogger<StripeBillingWebhookProcessor>.Instance);
    }

    private static string CreateStripeSignatureHeader(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private sealed class SqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteDatabase(SqliteConnection connection, DbContextOptions<WoodyDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public DbContextOptions<WoodyDbContext> Options { get; }

        public WoodyDbContext CreateContext() => new(Options);

        public static async Task<SqliteDatabase> CreateAsync()
        {
            var connection = new SqliteConnection($"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared");
            await connection.OpenAsync();
            await CreateSchemaAsync(connection);

            var options = new DbContextOptionsBuilder<WoodyDbContext>()
                .UseSqlite(connection.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

            return new SqliteDatabase(connection, options);
        }

        private static async Task CreateSchemaAsync(SqliteConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE billing_webhook_receipts (
                    event_id TEXT PRIMARY KEY,
                    event_type TEXT NOT NULL,
                    received_at_utc TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
