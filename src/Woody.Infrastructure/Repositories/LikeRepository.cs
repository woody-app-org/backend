using Microsoft.EntityFrameworkCore;
using Npgsql;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class LikeRepository : ILikeRepository
{
    private const string UniqueViolationSqlState = "23505";
    private const string LikeUniqueIndexName = "ix_likes_user_id_target_type_target_id";

    private readonly WoodyDbContext _db;

    public LikeRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<int, int>> GetPostLikeCountsAsync(IReadOnlyList<int> postIds, CancellationToken cancellationToken = default)
    {
        if (postIds.Count == 0)
            return new Dictionary<int, int>();

        return await _db.Likes.AsNoTracking()
            .Where(l => l.TargetType == LikeTargetType.Post && postIds.Contains(l.TargetId))
            .GroupBy(l => l.TargetId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
    }

    public async Task<HashSet<int>> GetLikedPostIdsForViewerAsync(int viewerUserId, IReadOnlyList<int> postIds, CancellationToken cancellationToken = default)
    {
        if (postIds.Count == 0)
            return new HashSet<int>();

        var list = await _db.Likes.AsNoTracking()
            .Where(l => l.UserId == viewerUserId && l.TargetType == LikeTargetType.Post && postIds.Contains(l.TargetId))
            .Select(l => l.TargetId)
            .ToListAsync(cancellationToken);
        return list.ToHashSet();
    }

    public Task<bool> ExistsPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default) =>
        _db.Likes.AnyAsync(
            l => l.UserId == userId && l.TargetType == LikeTargetType.Post && l.TargetId == postId,
            cancellationToken);

    public async Task<bool> TryAddPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default)
    {
        var like = new Like
        {
            UserId = userId,
            TargetType = LikeTargetType.Post,
            TargetId = postId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Likes.Add(like);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueLikeViolation(ex))
        {
            Detach(like);
            return false;
        }
    }

    public void Add(Like like) => _db.Likes.Add(like);

    public async Task<Like?> GetPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default) =>
        await _db.Likes.FirstOrDefaultAsync(
            l => l.UserId == userId && l.TargetType == LikeTargetType.Post && l.TargetId == postId,
            cancellationToken);

    public void Remove(Like like) => _db.Likes.Remove(like);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private static bool IsUniqueLikeViolation(DbUpdateException ex) =>
        IsPostgresUniqueLikeViolation(ex) || IsSqliteUniqueViolation(ex);

    private static bool IsPostgresUniqueLikeViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == UniqueViolationSqlState
        && (pg.ConstraintName == null
            || string.Equals(pg.ConstraintName, LikeUniqueIndexName, StringComparison.OrdinalIgnoreCase));

    private static bool IsSqliteUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException?.GetType().FullName != "Microsoft.Data.Sqlite.SqliteException")
            return false;

        var errorCode = ex.InnerException.GetType().GetProperty("SqliteErrorCode")?.GetValue(ex.InnerException);
        return errorCode is 19;
    }

    private void Detach(Like like)
    {
        var entry = _db.Entry(like);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
