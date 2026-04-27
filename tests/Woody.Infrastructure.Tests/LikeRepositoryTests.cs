using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class LikeRepositoryTests
{
    [Fact]
    public async Task TryAddPostLikeAsync_InsertsOnceAndTreatsDuplicateAsExisting()
    {
        await using var database = await SqliteDatabase.CreateAsync();

        await using var firstDb = new WoodyDbContext(database.Options);
        var firstRepo = new LikeRepository(firstDb);
        var firstResult = await firstRepo.TryAddPostLikeAsync(userId: 1, postId: 42);

        await using var secondDb = new WoodyDbContext(database.Options);
        var secondRepo = new LikeRepository(secondDb);
        var secondResult = await secondRepo.TryAddPostLikeAsync(userId: 1, postId: 42);

        await using var assertDb = new WoodyDbContext(database.Options);
        var count = await assertDb.Likes.CountAsync(l =>
            l.UserId == 1 && l.TargetType == LikeTargetType.Post && l.TargetId == 42);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task TryAddPostLikeAsync_HandlesConcurrentDuplicateLikes()
    {
        await using var database = await SqliteDatabase.CreateAsync();

        var results = await Task.WhenAll(
            AddLikeWithNewContextAsync(database.Options, userId: 1, postId: 99),
            AddLikeWithNewContextAsync(database.Options, userId: 1, postId: 99));

        await using var assertDb = new WoodyDbContext(database.Options);
        var count = await assertDb.Likes.CountAsync(l =>
            l.UserId == 1 && l.TargetType == LikeTargetType.Post && l.TargetId == 99);

        Assert.Contains(true, results);
        Assert.Contains(false, results);
        Assert.Equal(1, count);
    }

    private static async Task<bool> AddLikeWithNewContextAsync(
        DbContextOptions<WoodyDbContext> options,
        int userId,
        int postId)
    {
        await using var db = new WoodyDbContext(options);
        var repo = new LikeRepository(db);
        return await repo.TryAddPostLikeAsync(userId, postId);
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
                .Options;

            return new SqliteDatabase(connection, options);
        }

        private static async Task CreateSchemaAsync(SqliteConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE Likes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    TargetType INTEGER NOT NULL,
                    TargetId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                );

                CREATE UNIQUE INDEX ix_likes_user_id_target_type_target_id
                    ON Likes (UserId, TargetType, TargetId);
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
