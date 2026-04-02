using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/feed")]
public class FeedController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public FeedController(WoodyDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDto<PostResponseDto>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string filter = "trending",
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var posts = await _db.Posts.AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .ToListAsync(cancellationToken);

        if (posts.Count == 0)
        {
            return Ok(new PaginatedResponseDto<PostResponseDto>
            {
                Items = new List<PostResponseDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                HasNextPage = false,
                HasPreviousPage = false
            });
        }

        var postIds = posts.Select(p => p.Id).ToList();

        var likeCounts = await _db.Likes.AsNoTracking()
            .Where(l => l.TargetType == LikeTargetType.Post && postIds.Contains(l.TargetId))
            .GroupBy(l => l.TargetId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var commentCounts = await _db.Comments.AsNoTracking()
            .Where(c => postIds.Contains(c.PostId) && c.DeletedAt == null)
            .GroupBy(c => c.PostId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        int Score(int pid) => likeCounts.GetValueOrDefault(pid) + 2 * commentCounts.GetValueOrDefault(pid);

        IEnumerable<Post> ordered = posts;

        if (string.Equals(filter, "following", StringComparison.OrdinalIgnoreCase) && viewerId.HasValue)
        {
            var following = (await _db.Follows.AsNoTracking()
                    .Where(f => f.FollowingUserId == viewerId.Value)
                    .Select(f => f.FollowedUserId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            ordered = posts.Where(p => following.Contains(p.UserId))
                .OrderByDescending(p => Score(p.Id));
        }
        else if (string.Equals(filter, "forYou", StringComparison.OrdinalIgnoreCase) && viewerId.HasValue)
        {
            var mine = (await _db.CommunityMemberships.AsNoTracking()
                    .Where(m => m.UserId == viewerId.Value && m.Status == "active")
                    .Select(m => m.CommunityId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            var fromJoined = posts.Where(p => mine.Contains(p.CommunityId)).OrderByDescending(p => Score(p.Id)).ToList();
            var discover = posts.Where(p => !mine.Contains(p.CommunityId)).OrderByDescending(p => Score(p.Id)).ToList();
            var seen = new HashSet<int>();
            var merged = new List<Post>();
            foreach (var p in fromJoined.Concat(discover))
            {
                if (seen.Add(p.Id))
                    merged.Add(p);
            }

            ordered = merged;
        }
        else
            ordered = posts.OrderByDescending(p => Score(p.Id));

        var list = ordered.ToList();
        var total = list.Count;
        var slice = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = await PostEnricher.ToPostDtosAsync(_db, slice, viewerId, cancellationToken);

        return Ok(new PaginatedResponseDto<PostResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        });
    }
}
