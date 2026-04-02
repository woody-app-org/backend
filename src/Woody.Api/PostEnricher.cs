using Microsoft.EntityFrameworkCore;
using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Mapping;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api;

public static class PostEnricher
{
    public static async Task<List<PostResponseDto>> ToPostDtosAsync(
        WoodyDbContext db,
        IReadOnlyList<Post> posts,
        int? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (posts.Count == 0)
            return new List<PostResponseDto>();

        var ids = posts.Select(p => p.Id).ToList();

        var likeCounts = await db.Likes.AsNoTracking()
            .Where(l => l.TargetType == LikeTargetType.Post && ids.Contains(l.TargetId))
            .GroupBy(l => l.TargetId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);

        var commentCounts = await db.Comments.AsNoTracking()
            .Where(c => ids.Contains(c.PostId) && c.DeletedAt == null)
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);

        HashSet<int> liked = new();
        if (viewerUserId.HasValue)
        {
            liked = (await db.Likes.AsNoTracking()
                    .Where(l => l.UserId == viewerUserId.Value && l.TargetType == LikeTargetType.Post && ids.Contains(l.TargetId))
                    .Select(l => l.TargetId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        return posts.Select(p =>
        {
            var lc = likeCounts.GetValueOrDefault(p.Id);
            var cc = commentCounts.GetValueOrDefault(p.Id);
            var isLiked = viewerUserId.HasValue && liked.Contains(p.Id);
            return EntityMappers.ToPostDto(p, lc, cc, viewerUserId, isLiked);
        }).ToList();
    }
}
