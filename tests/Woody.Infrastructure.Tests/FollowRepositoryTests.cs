using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class FollowRepositoryTests
{
    [Fact]
    public async Task ListFollowersPagedAsync_WithoutSearch_ReturnsPaginatedFollowers()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(followedUserId: 1, page: 1, pageSize: 2);

        Assert.Equal(3, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task ListFollowersPagedAsync_SearchByUsername_ReturnsMatchingFollowers()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 10, search: "ana_souza");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("ana_souza", items[0].Username);
    }

    [Fact]
    public async Task ListFollowersPagedAsync_SearchByDisplayName_ReturnsMatchingFollowers()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 10, search: "souza");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("Ana Souza", items[0].DisplayName);
    }

    [Theory]
    [InlineData("ana")]
    public async Task ListFollowersPagedAsync_SearchIsCaseInsensitiveForStoredNames(string search)
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 10, search: search);

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, u => u.Username == "ana_souza");
        Assert.Contains(items, u => u.Username == "mariana_ok");
    }

    [Fact]
    public async Task ListFollowersPagedAsync_SearchWithNoMatches_ReturnsEmptyAndZeroTotal()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 10, search: "inexistente");

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ListFollowersPagedAsync_SearchPaginatesFilteredResults()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (page1Items, page1Total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 1, search: "ana");
        var (page2Items, page2Total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 2, pageSize: 1, search: "ana");

        Assert.Equal(2, page1Total);
        Assert.Equal(2, page2Total);
        Assert.Single(page1Items);
        Assert.Single(page2Items);
        Assert.NotEqual(page1Items[0].Id, page2Items[0].Id);
    }

    [Fact]
    public async Task ListFollowersPagedAsync_SearchDoesNotMatchEmailOrUsersOutsideFollowList()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 10, search: "secret");

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ListFollowersPagedAsync_SearchDoesNotReturnMatchingUsersOutsideFollowRelation()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowersPagedAsync(
            followedUserId: 1, page: 1, pageSize: 10, search: "outsider");

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ListFollowingPagedAsync_WithoutSearch_ReturnsPaginatedFollowing()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowingPagedAsync(followingUserId: 1, page: 1, pageSize: 2);

        Assert.Equal(3, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task ListFollowingPagedAsync_SearchByUsername_ReturnsMatchingAccounts()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowingPagedAsync(
            followingUserId: 1, page: 1, pageSize: 10, search: "diana");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("diana_ana", items[0].Username);
    }

    [Fact]
    public async Task ListFollowingPagedAsync_SearchByDisplayName_ReturnsMatchingAccounts()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowingPagedAsync(
            followingUserId: 1, page: 1, pageSize: 10, search: "carla");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("carla", items[0].Username);
    }

    [Fact]
    public async Task ListFollowingPagedAsync_SearchMatchesUsernameSubstring()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowingPagedAsync(
            followingUserId: 1, page: 1, pageSize: 10, search: "ana");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("diana_ana", items[0].Username);
    }

    [Fact]
    public async Task ListFollowingPagedAsync_SearchWithNoMatches_ReturnsEmptyAndZeroTotal()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (items, total) = await repo.ListFollowingPagedAsync(
            followingUserId: 1, page: 1, pageSize: 10, search: "inexistente");

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ListFollowingPagedAsync_SearchPaginatesFilteredResults()
    {
        await using var database = await FollowSqliteDatabase.CreateAsync();
        await SeedFollowGraphAsync(database.Options);

        await using var db = new WoodyDbContext(database.Options);
        var repo = new FollowRepository(db);

        var (page1Items, page1Total) = await repo.ListFollowingPagedAsync(
            followingUserId: 1, page: 1, pageSize: 1, search: "a");
        var (page2Items, page2Total) = await repo.ListFollowingPagedAsync(
            followingUserId: 1, page: 2, pageSize: 1, search: "a");

        Assert.Equal(2, page1Total);
        Assert.Equal(2, page2Total);
        Assert.Single(page1Items);
        Assert.Single(page2Items);
        Assert.NotEqual(page1Items[0].Id, page2Items[0].Id);
    }

    private static async Task SeedFollowGraphAsync(DbContextOptions<WoodyDbContext> options)
    {
        await using var db = new WoodyDbContext(options);
        var now = DateTime.UtcNow;

        db.Users.AddRange(
            CreateUser(1, "profile_user", "Profile User", "profile@example.com", now),
            CreateUser(2, "ana_souza", "Ana Souza", "ana.secret@example.com", now),
            CreateUser(3, "mariana_ok", "Mariana", "m@example.com", now),
            CreateUser(4, "bob_user", "Bob", "ana@example.com", now),
            CreateUser(5, "ana_outsider", "Ana Outsider", "outsider@example.com", now),
            CreateUser(10, "carla", "Carla", "carla@example.com", now),
            CreateUser(11, "diana_ana", "Diana", "diana@example.com", now),
            CreateUser(12, "ze_user", "Zé", "ze@example.com", now));

        db.Follows.AddRange(
            new Follow { FollowingUserId = 2, FollowedUserId = 1, CreatedAt = now },
            new Follow { FollowingUserId = 3, FollowedUserId = 1, CreatedAt = now.AddMinutes(1) },
            new Follow { FollowingUserId = 4, FollowedUserId = 1, CreatedAt = now.AddMinutes(2) },
            new Follow { FollowingUserId = 1, FollowedUserId = 10, CreatedAt = now.AddMinutes(3) },
            new Follow { FollowingUserId = 1, FollowedUserId = 11, CreatedAt = now.AddMinutes(4) },
            new Follow { FollowingUserId = 1, FollowedUserId = 12, CreatedAt = now.AddMinutes(5) });

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

    private sealed class FollowSqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public DbContextOptions<WoodyDbContext> Options { get; }

        private FollowSqliteDatabase(SqliteConnection connection, DbContextOptions<WoodyDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public static async Task<FollowSqliteDatabase> CreateAsync()
        {
            var connection = new SqliteConnection($"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<WoodyDbContext>()
                .UseSqlite(connection)
                .UseSnakeCaseNamingConvention()
                .Options;

            await using (var db = new WoodyDbContext(options))
                await db.Database.EnsureCreatedAsync();

            return new FollowSqliteDatabase(connection, options);
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }
}
