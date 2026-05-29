using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Application.Tests;

public class UserRelationshipVisibilityServiceTests
{
    [Fact]
    public async Task BlockAsync_Throws_WhenBlockingSelf()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var svc = CreateService(database);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.BlockAsync(1, 1));
    }

    [Fact]
    public async Task BlockAsync_Throws_WhenBlockedUserDoesNotExist()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.BlockAsync(1, 999));
    }

    [Fact]
    public async Task BlockAsync_CreatesUserBlock()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);

        await using var db = new WoodyDbContext(database.Options);
        Assert.True(await db.UserBlocks.AnyAsync(b => b.BlockerUserId == 1 && b.BlockedUserId == 2));
    }

    [Fact]
    public async Task BlockAsync_IsIdempotent()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);
        await svc.BlockAsync(1, 2);

        await using var db = new WoodyDbContext(database.Options);
        Assert.Equal(1, await db.UserBlocks.CountAsync(b => b.BlockerUserId == 1 && b.BlockedUserId == 2));
    }

    [Fact]
    public async Task UnblockAsync_RemovesUserBlock()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);
        await svc.UnblockAsync(1, 2);

        await using var db = new WoodyDbContext(database.Options);
        Assert.False(await db.UserBlocks.AnyAsync(b => b.BlockerUserId == 1 && b.BlockedUserId == 2));
    }

    [Fact]
    public async Task UnblockAsync_IsIdempotent()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.UnblockAsync(1, 2);
        await svc.UnblockAsync(1, 2);
    }

    [Fact]
    public async Task BlockAsync_RemovesFollowBothWays()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        await SeedUsersAsync(database.Options);
        await SeedMutualFollowAsync(database.Options, now);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);

        await using var db = new WoodyDbContext(database.Options);
        Assert.False(await db.Follows.AnyAsync());
    }

    [Fact]
    public async Task UnblockAsync_DoesNotRestoreFollow()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        await SeedUsersAsync(database.Options);
        await SeedMutualFollowAsync(database.Options, now);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);
        await svc.UnblockAsync(1, 2);

        await using var db = new WoodyDbContext(database.Options);
        Assert.False(await db.Follows.AnyAsync());
    }

    [Fact]
    public async Task AreUsersBlockedEitherWayAsync_ReturnsTrueForBothDirections()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);

        Assert.True(await svc.AreUsersBlockedEitherWayAsync(1, 2));
        Assert.True(await svc.AreUsersBlockedEitherWayAsync(2, 1));
    }

    [Fact]
    public async Task GetHiddenUserIdsForViewerAsync_ReturnsBothDirections()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);
        await svc.BlockAsync(3, 1);

        var hiddenForOne = await svc.GetHiddenUserIdsForViewerAsync(1);

        Assert.Contains(2, hiddenForOne);
        Assert.Contains(3, hiddenForOne);
    }

    [Fact]
    public async Task ListBlockedByUserPagedAsync_ReturnsOnlyUsersBlockedByViewer()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var svc = CreateService(database);
        await svc.BlockAsync(1, 2);
        await svc.BlockAsync(3, 1);

        var page = await svc.ListBlockedByUserPagedAsync(1, page: 1, pageSize: 20, search: null);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("2", page.Items[0].Id);
    }

    [Fact]
    public async Task ProfileSignalSocialGate_UsesPersistedBlocks()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var visibility = CreateService(database);
        await visibility.BlockAsync(1, 2);

        IProfileSignalSocialGate gate = new ProfileSignalSocialGate(visibility);

        Assert.True(await gate.AreUsersBlockedEitherWayAsync(1, 2));
        Assert.True(await gate.AreUsersBlockedEitherWayAsync(2, 1));
    }

    [Fact]
    public async Task StoriesService_RespectsPersistedBlockViaGate()
    {
        await using var database = await UserBlockSqliteDatabase.CreateAsync();
        await SeedUsersAsync(database.Options);

        var visibility = CreateService(database);
        await visibility.BlockAsync(1, 2);

        var users = new UserRepository(new WoodyDbContext(database.Options));
        var stories = new Mock<IStoryRepository>();
        IProfileSignalSocialGate gate = new ProfileSignalSocialGate(visibility);
        var svc = new StoriesService(
            stories.Object,
            users,
            new FollowRepository(new WoodyDbContext(database.Options)),
            new Mock<IMediaStorageProvider>().Object,
            gate);

        var result = await svc.GetActiveStoriesByUserAsync(targetUserId: 2, viewerUserId: 1);

        Assert.Empty(result);
        stories.Verify(
            s => s.ListActiveByAuthorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static UserRelationshipVisibilityService CreateService(UserBlockSqliteDatabase database)
    {
        var db = new WoodyDbContext(database.Options);
        return CreateService(db);
    }

    private static UserRelationshipVisibilityService CreateService(WoodyDbContext db)
    {
        var stories = new Mock<IStoryRepository>();
        stories.Setup(s => s.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        return new UserRelationshipVisibilityService(
            new UserBlockRepository(db),
            new FollowRepository(db),
            new UserRepository(db),
            stories.Object);
    }

    private static async Task SeedUsersAsync(DbContextOptions<WoodyDbContext> options)
    {
        await using var db = new WoodyDbContext(options);
        var now = DateTime.UtcNow;
        db.Users.AddRange(
            CreateUser(1, "blocker", "Blocker", "blocker@example.com", now),
            CreateUser(2, "blocked_user", "Blocked User", "blocked@example.com", now),
            CreateUser(3, "third_user", "Third", "third@example.com", now));
        await db.SaveChangesAsync();
    }

    private static async Task SeedMutualFollowAsync(DbContextOptions<WoodyDbContext> options, DateTime now)
    {
        await using var db = new WoodyDbContext(options);
        db.Follows.AddRange(
            new Follow { FollowingUserId = 1, FollowedUserId = 2, CreatedAt = now },
            new Follow { FollowingUserId = 2, FollowedUserId = 1, CreatedAt = now });
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

    private sealed class UserBlockSqliteDatabase : IAsyncDisposable
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
