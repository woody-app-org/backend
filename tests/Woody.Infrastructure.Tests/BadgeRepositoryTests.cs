using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class BadgeRepositoryTests
{
    [Fact]
    public async Task TryAddUserBadgeAsync_InsertsOnceAndTreatsDuplicateAsExisting()
    {
        await using var database = await BadgeSqliteDatabase.CreateAsync();
        var badgeId = await SeedBadgeAsync(database.Options, slug: "seed");

        await using var firstDb = new WoodyDbContext(database.Options);
        var firstRepo = new BadgeRepository(firstDb);
        var firstResult = await firstRepo.TryAddUserBadgeAsync(userId: 1, badgeId, DateTime.UtcNow);

        await using var secondDb = new WoodyDbContext(database.Options);
        var secondRepo = new BadgeRepository(secondDb);
        var secondResult = await secondRepo.TryAddUserBadgeAsync(userId: 1, badgeId, DateTime.UtcNow);

        await using var assertDb = new WoodyDbContext(database.Options);
        var count = await assertDb.UserBadges.CountAsync(ub => ub.UserId == 1 && ub.BadgeId == badgeId);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task TryAddUserBadgeAsync_HandlesConcurrentDuplicateAwards()
    {
        await using var database = await BadgeSqliteDatabase.CreateAsync();
        var badgeId = await SeedBadgeAsync(database.Options, slug: "seed");

        var results = await Task.WhenAll(
            AwardWithNewContextAsync(database.Options, userId: 1, badgeId),
            AwardWithNewContextAsync(database.Options, userId: 1, badgeId));

        await using var assertDb = new WoodyDbContext(database.Options);
        var count = await assertDb.UserBadges.CountAsync(ub => ub.UserId == 1 && ub.BadgeId == badgeId);

        Assert.Contains(true, results);
        Assert.Contains(false, results);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetActiveUserBadgesAsync_ExcludesInactiveBadges()
    {
        await using var database = await BadgeSqliteDatabase.CreateAsync();
        var activeId = await SeedBadgeAsync(database.Options, slug: "active", isActive: true, sortOrder: 1);
        var inactiveId = await SeedBadgeAsync(database.Options, slug: "inactive", isActive: false, sortOrder: 2);

        await using (var db = new WoodyDbContext(database.Options))
        {
            db.UserBadges.AddRange(
                new UserBadge { UserId = 1, BadgeId = activeId, EarnedAt = DateTime.UtcNow.AddDays(-1) },
                new UserBadge { UserId = 1, BadgeId = inactiveId, EarnedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        await using var readDb = new WoodyDbContext(database.Options);
        var repo = new BadgeRepository(readDb);
        var badges = await repo.GetActiveUserBadgesAsync(1);

        Assert.Single(badges);
        Assert.Equal("active", badges[0].Badge.Slug);
    }

    private static async Task<bool> AwardWithNewContextAsync(
        DbContextOptions<WoodyDbContext> options,
        int userId,
        int badgeId)
    {
        await using var db = new WoodyDbContext(options);
        var repo = new BadgeRepository(db);
        return await repo.TryAddUserBadgeAsync(userId, badgeId, DateTime.UtcNow);
    }

    private static async Task<int> SeedBadgeAsync(
        DbContextOptions<WoodyDbContext> options,
        string slug,
        bool isActive = true,
        int sortOrder = 10)
    {
        await using var db = new WoodyDbContext(options);
        var badge = new Badge
        {
            Slug = slug,
            Name = slug,
            Description = "test",
            Category = "test",
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
        db.Badges.Add(badge);
        await db.SaveChangesAsync();
        return badge.Id;
    }

    private sealed class BadgeSqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private BadgeSqliteDatabase(SqliteConnection connection, DbContextOptions<WoodyDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public DbContextOptions<WoodyDbContext> Options { get; }

        public static async Task<BadgeSqliteDatabase> CreateAsync()
        {
            var connection = new SqliteConnection($"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared");
            await connection.OpenAsync();
            await CreateSchemaAsync(connection);

            var options = new DbContextOptionsBuilder<WoodyDbContext>()
                .UseSqlite(connection)
                .Options;

            return new BadgeSqliteDatabase(connection, options);
        }

        private static async Task CreateSchemaAsync(SqliteConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE badges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Slug TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    IconAssetKey TEXT NULL,
                    Category TEXT NOT NULL,
                    Rarity TEXT NULL,
                    IsActive INTEGER NOT NULL,
                    SortOrder INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                );

                CREATE UNIQUE INDEX ix_badges_slug ON badges (Slug);

                CREATE TABLE user_badges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    BadgeId INTEGER NOT NULL,
                    EarnedAt TEXT NOT NULL,
                    MetadataJson TEXT NULL
                );

                CREATE UNIQUE INDEX ix_user_badges_user_id_badge_id
                    ON user_badges (UserId, BadgeId);
                CREATE INDEX ix_user_badges_user_id ON user_badges (UserId);
                CREATE INDEX ix_user_badges_badge_id ON user_badges (BadgeId);
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
