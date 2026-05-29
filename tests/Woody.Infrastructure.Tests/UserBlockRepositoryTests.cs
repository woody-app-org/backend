using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class UserBlockRepositoryTests
{
    [Fact]
    public async Task AreBlockedEitherWayAsync_ReturnsTrue_WhenEitherDirectionBlocked()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options, now: DateTime.UtcNow);
        await SeedBlockAsync(database.Options, blockerUserId: 1, blockedUserId: 2);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new UserBlockRepository(db);

        Assert.True(await repo.AreBlockedEitherWayAsync(1, 2));
        Assert.True(await repo.AreBlockedEitherWayAsync(2, 1));
        Assert.False(await repo.AreBlockedEitherWayAsync(1, 3));
    }

    [Fact]
    public async Task GetHiddenUserIdsForViewerAsync_IncludesBlockedByMeAndWhoBlockedMe()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        await SeedUsersAsync(database.Options, now);
        await SeedBlockAsync(database.Options, blockerUserId: 1, blockedUserId: 2);
        await SeedBlockAsync(database.Options, blockerUserId: 3, blockedUserId: 1);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new UserBlockRepository(db);

        var hidden = await repo.GetHiddenUserIdsForViewerAsync(1);

        Assert.Equal(2, hidden.Count);
        Assert.Contains(2, hidden);
        Assert.Contains(3, hidden);
    }

    [Fact]
    public async Task ListBlockedUsersPagedAsync_ReturnsOnlyUsersBlockedByViewer()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        await SeedUsersAsync(database.Options, now);
        await SeedBlockAsync(database.Options, blockerUserId: 1, blockedUserId: 2);
        await SeedBlockAsync(database.Options, blockerUserId: 3, blockedUserId: 1);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new UserBlockRepository(db);

        var (items, total) = await repo.ListBlockedUsersPagedAsync(blockerUserId: 1, page: 1, pageSize: 10);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal(2, items[0].Id);
        Assert.Equal("blocked_user", items[0].Username);
    }

    [Fact]
    public async Task ListBlockedUsersPagedAsync_SearchByUsername_FiltersResults()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        await SeedUsersAsync(database.Options, now);
        await SeedBlockAsync(database.Options, blockerUserId: 1, blockedUserId: 2);
        await SeedBlockAsync(database.Options, blockerUserId: 1, blockedUserId: 3);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new UserBlockRepository(db);

        var (items, total) = await repo.ListBlockedUsersPagedAsync(
            blockerUserId: 1, page: 1, pageSize: 10, search: "ana");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("ana_blocked", items[0].Username);
    }

    [Fact]
    public async Task UniqueIndex_PreventsDuplicateBlockRows()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        await SeedUsersAsync(database.Options, now);
        await SeedBlockAsync(database.Options, blockerUserId: 1, blockedUserId: 2);

        await using var db = new WoodyDbContext(database.Options);
        db.UserBlocks.Add(new UserBlock
        {
            BlockerUserId = 1,
            BlockedUserId = 2,
            CreatedAt = now
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task SeedUsersAsync(DbContextOptions<WoodyDbContext> options, DateTime now)
    {
        await using var db = new WoodyDbContext(options);
        db.Users.AddRange(
            CreateUser(1, "blocker", "Blocker", "blocker@example.com", now),
            CreateUser(2, "blocked_user", "Blocked User", "blocked@example.com", now),
            CreateUser(3, "ana_blocked", "Ana Blocked", "ana@example.com", now));
        await db.SaveChangesAsync();
    }

    private static async Task SeedBlockAsync(
        DbContextOptions<WoodyDbContext> options,
        int blockerUserId,
        int blockedUserId)
    {
        await using var db = new WoodyDbContext(options);
        db.UserBlocks.Add(new UserBlock
        {
            BlockerUserId = blockerUserId,
            BlockedUserId = blockedUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static User CreateUser(int id, string username, string displayName, string email, DateTime createdAt) =>
        new()
        {
            Id = id,
            Username = username,
            DisplayName = displayName,
            Email = email,
            Password = "hash",
            Role = "User",
            CreatedAt = createdAt
        };

    internal sealed class UserBlockSqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public DbContextOptions<WoodyDbContext> Options { get; }

        private UserBlockSqliteDatabase(SqliteConnection connection, DbContextOptions<WoodyDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public static async Task<UserBlockSqliteDatabase> CreateAsync()
        {
            var connection = new SqliteConnection($"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<WoodyDbContext>()
                .UseSqlite(connection)
                .UseSnakeCaseNamingConvention()
                .Options;

            await using (var db = new WoodyDbContext(options))
                await db.Database.EnsureCreatedAsync();

            return new UserBlockSqliteDatabase(connection, options);
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }
}
