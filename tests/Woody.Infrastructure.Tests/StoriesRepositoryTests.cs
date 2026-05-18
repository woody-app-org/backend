using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woody.Application.Stories;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Stories;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class StoriesRepositoryTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task CreateWithActiveLimitAsync_RespectsActiveCountBeforeFourth(int existingActive, bool shouldCreate)
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;
        const int userId = 50;

        for (var i = 0; i < existingActive; i++)
            await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(i)));

        if (shouldCreate)
        {
            await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(existingActive + 10)));
            Assert.Equal(existingActive + 1, await CountActiveAsync(db, userId));
        }
        else
        {
            await Assert.ThrowsAsync<StoryLimitReachedException>(() =>
                repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(99))));
            Assert.Equal(3, await CountActiveAsync(db, userId));
        }
    }

    [Fact]
    public async Task CreateWithActiveLimitAsync_AllowsUpToThreeActiveStories()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;

        for (var i = 0; i < StoryPolicies.MaxActiveStoriesPerUser; i++)
            await repo.CreateWithActiveLimitAsync(CreateStory(1, now.AddMinutes(i)));

        await Assert.ThrowsAsync<StoryLimitReachedException>(() =>
            repo.CreateWithActiveLimitAsync(CreateStory(1, now.AddMinutes(10))));

        Assert.Equal(3, await CountActiveAsync(db, 1));
    }

    [Fact]
    public async Task CreateWithActiveLimitAsync_TwoActivePlusOneExpired_AllowsThirdActive()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;
        const int userId = 60;

        await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(1)));
        await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(2)));

        var expired = CreateStory(userId, now.AddMinutes(-30));
        expired.ExpiresAt = now.AddMinutes(-1);
        db.Stories.Add(expired);
        await db.SaveChangesAsync();

        await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(3)));
        Assert.Equal(3, await CountActiveAsync(db, userId));
    }

    [Fact]
    public async Task CreateWithActiveLimitAsync_TwoActivePlusOneDeleted_AllowsThirdActive()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;
        const int userId = 61;

        var first = await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(1)));
        await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(2)));
        await repo.SoftDeleteAsync(first, now, CancellationToken.None);

        await repo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(3)));
        Assert.Equal(2, await CountActiveAsync(db, userId));
    }

    [Fact]
    public async Task CreateWithActiveLimitAsync_ExpiredStoryDoesNotCountTowardLimit()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;

        var activeStories = new List<Story>();
        for (var i = 0; i < 3; i++)
            activeStories.Add(await repo.CreateWithActiveLimitAsync(CreateStory(1, now.AddMinutes(i))));

        activeStories[0].ExpiresAt = now.AddMinutes(-1);
        await db.SaveChangesAsync();

        var created = await repo.CreateWithActiveLimitAsync(CreateStory(1, now.AddMinutes(20)));
        Assert.NotNull(created);
        Assert.Equal(3, await CountActiveAsync(db, 1));
    }

    [Fact]
    public async Task CreateWithActiveLimitAsync_DeletedStoryFreesSlot()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;

        var stories = new List<Story>();
        for (var i = 0; i < 3; i++)
            stories.Add(await repo.CreateWithActiveLimitAsync(CreateStory(1, now.AddMinutes(i))));

        await repo.SoftDeleteAsync(stories[0], now, CancellationToken.None);

        var replacement = await repo.CreateWithActiveLimitAsync(CreateStory(1, now.AddMinutes(30)));
        Assert.NotNull(replacement);
    }

    [Fact]
    public async Task ListActiveByAuthor_ExcludesExpiredAndDeleted()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;

        var active = await repo.CreateWithActiveLimitAsync(CreateStory(2, now));

        var expired = CreateStory(2, now.AddHours(-2));
        expired.ExpiresAt = now.AddMinutes(-5);
        db.Stories.Add(expired);

        var deleted = CreateStory(2, now.AddHours(-1));
        deleted.DeletedAt = now;
        db.Stories.Add(deleted);

        await db.SaveChangesAsync();

        var list = await repo.ListActiveByAuthorAsync(2);
        Assert.Single(list);
        Assert.Equal(active.Id, list[0].Id);
    }

    [Fact]
    public async Task TryRegisterViewAsync_IsIdempotent()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;
        var story = await repo.CreateWithActiveLimitAsync(CreateStory(3, now));

        Assert.True(await repo.TryRegisterViewAsync(story.Id, 10, now));
        Assert.False(await repo.TryRegisterViewAsync(story.Id, 10, now.AddMinutes(1)));

        Assert.Equal(1, await db.StoryViews.CountAsync(v => v.StoryId == story.Id && v.ViewerUserId == 10));
    }

    [Fact]
    public async Task GetUserIdsWithActiveStoriesAsync_ReturnsOnlyUsersWithActiveStories()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        var now = DateTime.UtcNow;

        await repo.CreateWithActiveLimitAsync(CreateStory(7, now));
        var expired = CreateStory(8, now);
        expired.ExpiresAt = now.AddMinutes(-1);
        db.Stories.Add(expired);
        await db.SaveChangesAsync();

        var ids = await repo.GetUserIdsWithActiveStoriesAsync([7, 8, 9]);
        Assert.Contains(7, ids);
        Assert.DoesNotContain(8, ids);
        Assert.DoesNotContain(9, ids);
    }

    /// <summary>
    /// Cenário A: 2 ativos + 2 requests paralelas → só 1 nova; total final = 3.
    /// </summary>
    [Fact]
    public async Task CreateWithActiveLimitAsync_Concurrent_WhenTwoActive_OnlyOneMoreSucceeds()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var seedDb = database.CreateContext();
        var seedRepo = new StoryRepository(seedDb);
        var now = DateTime.UtcNow;
        const int userId = 200;

        await seedRepo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(1)));
        await seedRepo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(2)));
        Assert.Equal(2, await CountActiveAsync(seedDb, userId));

        var results = await Task.WhenAll(
            TryCreateAsync(database, userId, now.AddMinutes(10)),
            TryCreateAsync(database, userId, now.AddMinutes(11)));

        await using var assertDb = database.CreateContext();
        var finalActive = await CountActiveAsync(assertDb, userId);

        Assert.Equal(3, finalActive);
        Assert.Equal(1, results.Count(r => r.Success));
        Assert.Equal(1, results.Count(r => !r.Success && r.LimitReached));
    }

    /// <summary>
    /// Cenário B: 3 ativos + N requests paralelas → nenhuma nova; total final = 3.
    /// </summary>
    [Fact]
    public async Task CreateWithActiveLimitAsync_Concurrent_WhenThreeActive_NoneSucceed()
    {
        await using var database = await StoriesSqliteDatabase.CreateAsync();
        await using var seedDb = database.CreateContext();
        var seedRepo = new StoryRepository(seedDb);
        var now = DateTime.UtcNow;
        const int userId = 201;

        for (var i = 0; i < 3; i++)
            await seedRepo.CreateWithActiveLimitAsync(CreateStory(userId, now.AddMinutes(i)));

        Assert.Equal(3, await CountActiveAsync(seedDb, userId));

        var results = await Task.WhenAll(
            TryCreateAsync(database, userId, now.AddMinutes(20)),
            TryCreateAsync(database, userId, now.AddMinutes(21)),
            TryCreateAsync(database, userId, now.AddMinutes(22)),
            TryCreateAsync(database, userId, now.AddMinutes(23)));

        await using var assertDb = database.CreateContext();
        var finalActive = await CountActiveAsync(assertDb, userId);

        Assert.Equal(3, finalActive);
        Assert.All(results, r => Assert.False(r.Success));
        Assert.All(results, r => Assert.True(r.LimitReached));
    }

    private static async Task<(bool Success, bool LimitReached)> TryCreateAsync(
        StoriesSqliteDatabase database,
        int userId,
        DateTime createdAt)
    {
        await using var db = database.CreateContext();
        var repo = new StoryRepository(db);
        try
        {
            await repo.CreateWithActiveLimitAsync(CreateStory(userId, createdAt));
            return (true, false);
        }
        catch (StoryLimitReachedException)
        {
            return (false, true);
        }
    }

    private static Task<int> CountActiveAsync(WoodyDbContext db, int authorUserId)
    {
        var now = DateTime.UtcNow;
        return db.Stories.CountAsync(s =>
            s.AuthorUserId == authorUserId
            && s.DeletedAt == null
            && s.ExpiresAt > now);
    }

    private static Story CreateStory(int authorUserId, DateTime createdAt) => new()
    {
        AuthorUserId = authorUserId,
        MediaType = StoryMediaType.Text,
        Text = "story",
        CreatedAt = createdAt,
        ExpiresAt = createdAt.Add(StoryPolicies.StoryLifetime),
        Visibility = StoryVisibility.Public
    };

    private sealed class StoriesSqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public DbContextOptions<WoodyDbContext> Options { get; }

        private StoriesSqliteDatabase(SqliteConnection connection, DbContextOptions<WoodyDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public static async Task<StoriesSqliteDatabase> CreateAsync()
        {
            var connection = new SqliteConnection($"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared");
            await connection.OpenAsync();
            await CreateSchemaAsync(connection);

            var options = new DbContextOptionsBuilder<WoodyDbContext>()
                .UseSqlite(connection)
                .UseSnakeCaseNamingConvention()
                .Options;

            return new StoriesSqliteDatabase(connection, options);
        }

        public WoodyDbContext CreateContext() => new(Options);

        private static async Task CreateSchemaAsync(SqliteConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE stories (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    author_user_id INTEGER NOT NULL,
                    media_type INTEGER NOT NULL,
                    media_url TEXT NULL,
                    thumbnail_url TEXT NULL,
                    storage_key TEXT NULL,
                    text TEXT NULL,
                    background_color TEXT NULL,
                    created_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL,
                    deleted_at TEXT NULL,
                    visibility INTEGER NOT NULL DEFAULT 0,
                    music_provider TEXT NULL,
                    music_track_id TEXT NULL,
                    music_title TEXT NULL,
                    music_artist TEXT NULL,
                    music_preview_url TEXT NULL
                );

                CREATE TABLE story_views (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    story_id INTEGER NOT NULL,
                    viewer_user_id INTEGER NOT NULL,
                    viewed_at TEXT NOT NULL,
                    UNIQUE(story_id, viewer_user_id)
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }
}
