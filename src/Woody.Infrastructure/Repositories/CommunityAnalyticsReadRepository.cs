using Microsoft.EntityFrameworkCore;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommunityAnalyticsReadRepository : ICommunityAnalyticsReadRepository
{
    private readonly WoodyDbContext _db;

    public CommunityAnalyticsReadRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task<int> CountActiveMembershipsJoinedBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default) =>
        _db.CommunityMemberships.AsNoTracking()
            .CountAsync(
                m => m.CommunityId == communityId
                     && m.Status == "active"
                     && m.JoinedAt != null
                     && m.JoinedAt >= fromUtcInclusive
                     && m.JoinedAt <= toUtcInclusive,
                cancellationToken);

    public Task<int> CountPostsPublishedBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default) =>
        _db.Posts.AsNoTracking()
            .CountAsync(
                p => p.CommunityId == communityId
                     && p.DeletedAt == null
                     && p.CreatedAt >= fromUtcInclusive
                     && p.CreatedAt <= toUtcInclusive,
                cancellationToken);

    public Task<int> CountCommentsOnCommunityPostsBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default) =>
        _db.Comments.AsNoTracking()
            .CountAsync(
                c => c.DeletedAt == null
                     && c.CreatedAt >= fromUtcInclusive
                     && c.CreatedAt <= toUtcInclusive
                     && c.Post != null
                     && c.Post.CommunityId == communityId
                     && c.Post.DeletedAt == null,
                cancellationToken);

    public Task<int> CountLikesOnCommunityPostsBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default) =>
        _db.Likes.AsNoTracking()
            .CountAsync(
                l => l.TargetType == LikeTargetType.Post
                     && l.CreatedAt >= fromUtcInclusive
                     && l.CreatedAt <= toUtcInclusive
                     && _db.Posts.Any(p =>
                         p.Id == l.TargetId && p.CommunityId == communityId && p.DeletedAt == null),
                cancellationToken);

    public async Task<IReadOnlyList<CommunityTopPostAnalyticsRow>> GetTopCommunityPostsByScoreAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        int take,
        CancellationToken cancellationToken = default)
    {
        const int scanCap = 400;
        var posts = await _db.Posts.AsNoTracking()
            .Where(p => p.CommunityId == communityId && p.DeletedAt == null
                                           && p.CreatedAt >= fromUtcInclusive && p.CreatedAt <= toUtcInclusive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(scanCap)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.CreatedAt,
                AuthorUsername = p.User.Username
            })
            .ToListAsync(cancellationToken);

        if (posts.Count == 0)
            return Array.Empty<CommunityTopPostAnalyticsRow>();

        var ids = posts.Select(p => p.Id).ToList();
        var likeCounts = await _db.Likes.AsNoTracking()
            .Where(l => l.TargetType == LikeTargetType.Post && ids.Contains(l.TargetId))
            .GroupBy(l => l.TargetId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);

        var commentCounts = await _db.Comments.AsNoTracking()
            .Where(c => ids.Contains(c.PostId) && c.DeletedAt == null)
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);

        return posts
            .Select(p =>
            {
                var lc = likeCounts.GetValueOrDefault(p.Id);
                var cc = commentCounts.GetValueOrDefault(p.Id);
                return new CommunityTopPostAnalyticsRow
                {
                    PostId = p.Id,
                    Title = p.Title ?? string.Empty,
                    CreatedAtUtc = p.CreatedAt,
                    LikesCount = lc,
                    CommentsCount = cc,
                    AuthorUsername = p.AuthorUsername ?? string.Empty
                };
            })
            .OrderByDescending(r => r.LikesCount + r.CommentsCount)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<CommunityTagCountRow>> GetTopPostTagsInPeriodAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        int take,
        CancellationToken cancellationToken = default)
    {
        var tags = await _db.PostTags.AsNoTracking()
            .Where(t => t.Post.CommunityId == communityId
                        && t.Post.DeletedAt == null
                        && t.Post.CreatedAt >= fromUtcInclusive
                        && t.Post.CreatedAt <= toUtcInclusive)
            .Select(t => t.Tag)
            .ToListAsync(cancellationToken);

        if (tags.Count == 0)
            return Array.Empty<CommunityTagCountRow>();

        return tags
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .GroupBy(t => t.ToLowerInvariant())
            .Select(g => new CommunityTagCountRow { Tag = g.OrderBy(x => x.Length).First(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Tag)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<DateOnly, int>> CountPostsPerDayUtcAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Posts.AsNoTracking()
            .Where(p => p.CommunityId == communityId && p.DeletedAt == null
                                           && p.CreatedAt >= fromUtcInclusive && p.CreatedAt <= toUtcInclusive)
            .GroupBy(p => DateOnly.FromDateTime(p.CreatedAt.Kind == DateTimeKind.Utc
                ? p.CreatedAt
                : p.CreatedAt.ToUniversalTime()))
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Day, x => x.Count);
    }

    public async Task<IReadOnlyDictionary<DateOnly, int>> CountCommentsPerDayUtcAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Comments.AsNoTracking()
            .Where(c => c.DeletedAt == null
                        && c.CreatedAt >= fromUtcInclusive
                        && c.CreatedAt <= toUtcInclusive
                        && c.Post != null
                        && c.Post.CommunityId == communityId
                        && c.Post.DeletedAt == null)
            .GroupBy(c => DateOnly.FromDateTime(c.CreatedAt.Kind == DateTimeKind.Utc
                ? c.CreatedAt
                : c.CreatedAt.ToUniversalTime()))
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Day, x => x.Count);
    }

    public async Task<IReadOnlyDictionary<DateOnly, int>> CountNewMembersPerDayUtcAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.CommunityId == communityId
                        && m.Status == "active"
                        && m.JoinedAt != null
                        && m.JoinedAt >= fromUtcInclusive
                        && m.JoinedAt <= toUtcInclusive)
            .GroupBy(m => DateOnly.FromDateTime(m.JoinedAt!.Value.Kind == DateTimeKind.Utc
                ? m.JoinedAt.Value
                : m.JoinedAt.Value.ToUniversalTime()))
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Day, x => x.Count);
    }
}
