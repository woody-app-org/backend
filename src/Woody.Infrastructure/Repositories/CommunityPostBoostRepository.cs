using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommunityPostBoostRepository : ICommunityPostBoostRepository
{
    private readonly WoodyDbContext _db;

    public CommunityPostBoostRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task CancelActiveForPostAsync(int postId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var rows = await _db.CommunityPostBoosts
            .Where(b =>
                b.PostId == postId
                && b.CancelledAtUtc == null
                && b.StartedAtUtc <= utcNow
                && b.EndsAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var b in rows)
            b.CancelledAtUtc = utcNow;
    }

    public void Add(CommunityPostBoost boost) => _db.CommunityPostBoosts.Add(boost);

    public async Task<List<CommunityPostBoost>> ListActiveForCommunityAsync(
        int communityId,
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default) =>
        await _db.CommunityPostBoosts.AsNoTracking()
            .Include(b => b.Post)
            .Where(b =>
                b.CommunityId == communityId
                && b.Post.DeletedAt == null
                && b.CancelledAtUtc == null
                && b.StartedAtUtc <= utcNow
                && b.EndsAtUtc > utcNow)
            .OrderByDescending(b => b.EndsAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<CommunityPostBoost?> GetActiveForPostInCommunityAsync(
        int communityId,
        int postId,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        await _db.CommunityPostBoosts
            .FirstOrDefaultAsync(
                b => b.CommunityId == communityId
                     && b.PostId == postId
                     && b.CancelledAtUtc == null
                     && b.StartedAtUtc <= utcNow
                     && b.EndsAtUtc > utcNow,
                cancellationToken);

    public async Task<HashSet<int>> GetActiveBoostedPostIdsAmongAsync(
        IReadOnlyList<int> postIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (postIds.Count == 0)
            return new HashSet<int>();

        var set = postIds.ToHashSet();
        var list = await _db.CommunityPostBoosts.AsNoTracking()
            .Where(b =>
                set.Contains(b.PostId)
                && b.CancelledAtUtc == null
                && b.StartedAtUtc <= utcNow
                && b.EndsAtUtc > utcNow)
            .Select(b => b.PostId)
            .ToListAsync(cancellationToken);

        return list.ToHashSet();
    }

    public async Task<Dictionary<int, DateTime>> GetActiveBoostEndsByPostIdsAsync(
        IReadOnlyList<int> postIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (postIds.Count == 0)
            return new Dictionary<int, DateTime>();

        var set = postIds.ToHashSet();
        var rows = await _db.CommunityPostBoosts.AsNoTracking()
            .Where(b =>
                set.Contains(b.PostId)
                && b.CancelledAtUtc == null
                && b.StartedAtUtc <= utcNow
                && b.EndsAtUtc > utcNow)
            .Select(b => new { b.PostId, b.EndsAtUtc })
            .ToListAsync(cancellationToken);

        return rows.GroupBy(x => x.PostId).ToDictionary(g => g.Key, g => g.Max(x => x.EndsAtUtc));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
