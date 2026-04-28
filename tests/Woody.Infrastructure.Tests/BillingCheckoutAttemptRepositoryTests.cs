using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class BillingCheckoutAttemptRepositoryTests
{
    [Fact]
    public async Task ClaimOrGetAsync_CreatesAttemptThenReusesExistingKey()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;

        await using var firstDb = new WoodyDbContext(database.Options);
        var firstRepo = new BillingCheckoutAttemptRepository(firstDb);
        var first = await firstRepo.ClaimOrGetAsync(
            "checkout-key",
            10,
            BillingCheckoutAttemptSubjectKind.UserSubscription,
            "pro_monthly",
            null,
            now,
            now.AddHours(24));

        await using var secondDb = new WoodyDbContext(database.Options);
        var secondRepo = new BillingCheckoutAttemptRepository(secondDb);
        var second = await secondRepo.ClaimOrGetAsync(
            "checkout-key",
            10,
            BillingCheckoutAttemptSubjectKind.UserSubscription,
            "pro_monthly",
            null,
            now,
            now.AddHours(24));

        await using var assertDb = new WoodyDbContext(database.Options);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await assertDb.BillingCheckoutAttempts.CountAsync());
    }

    [Fact]
    public async Task ClaimOrGetAsync_HandlesConcurrentDuplicateClaims()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;

        var attempts = await Task.WhenAll(
            ClaimWithNewContextAsync(database.Options, now),
            ClaimWithNewContextAsync(database.Options, now));

        await using var assertDb = new WoodyDbContext(database.Options);

        Assert.Equal(attempts[0].Id, attempts[1].Id);
        Assert.Equal(1, await assertDb.BillingCheckoutAttempts.CountAsync());
    }

    private static async Task<Woody.Domain.Entities.BillingCheckoutAttempt> ClaimWithNewContextAsync(
        DbContextOptions<WoodyDbContext> options,
        DateTime now)
    {
        await using var db = new WoodyDbContext(options);
        var repo = new BillingCheckoutAttemptRepository(db);
        return await repo.ClaimOrGetAsync(
            "concurrent-checkout-key",
            10,
            BillingCheckoutAttemptSubjectKind.CommunityPremium,
            "community_premium_monthly",
            55,
            now,
            now.AddHours(24));
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
                CREATE TABLE billing_checkout_attempts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    idempotency_key TEXT NOT NULL,
                    user_id INTEGER NOT NULL,
                    subject_kind INTEGER NOT NULL,
                    plan_code TEXT NOT NULL,
                    community_id INTEGER NULL,
                    stripe_session_id TEXT NULL,
                    stripe_session_url TEXT NULL,
                    stripe_customer_id TEXT NULL,
                    status INTEGER NOT NULL,
                    expires_at_utc TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL
                );

                CREATE UNIQUE INDEX ix_billing_checkout_attempts_idempotency_key
                    ON billing_checkout_attempts (idempotency_key);
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
