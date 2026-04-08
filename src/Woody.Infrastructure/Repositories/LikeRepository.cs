using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class LikeRepository : ILikeRepository
{
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

    public void Add(Like like) => _db.Likes.Add(like);

    public async Task<Like?> GetPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default) =>
        await _db.Likes.FirstOrDefaultAsync(
            l => l.UserId == userId && l.TargetType == LikeTargetType.Post && l.TargetId == postId,
            cancellationToken);

    public void Remove(Like like) => _db.Likes.Remove(like);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
