using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class FeedService : IFeedService
{
    private readonly IPostRepository _posts;
    private readonly IFollowRepository _follows;
    private readonly ICommunityMembershipRepository _memberships;
    private readonly ILikeRepository _likes;
    private readonly ICommentRepository _comments;
    private readonly IPostEnrichmentService _enrichment;

    public FeedService(
        IPostRepository posts,
        IFollowRepository follows,
        ICommunityMembershipRepository memberships,
        ILikeRepository likes,
        ICommentRepository comments,
        IPostEnrichmentService enrichment)
    {
        _posts = posts;
        _follows = follows;
        _memberships = memberships;
        _likes = likes;
        _comments = comments;
        _enrichment = enrichment;
    }

    public async Task<PaginatedResponseDto<PostResponseDto>> GetFeedAsync(
        int page,
        int pageSize,
        string filter,
        int? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var posts = await _posts.ListNonDeletedWithNavAsync(cancellationToken);

        if (posts.Count == 0)
        {
            return new PaginatedResponseDto<PostResponseDto>
            {
                Items = new List<PostResponseDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                HasNextPage = false,
                HasPreviousPage = false
            };
        }

        var postIds = posts.Select(p => p.Id).ToList();

        var likeCounts = await _likes.GetPostLikeCountsAsync(postIds, cancellationToken);
        var commentCounts = await _comments.GetActiveCommentCountsByPostIdsAsync(postIds, cancellationToken);

        int Score(int pid) => likeCounts.GetValueOrDefault(pid) + 2 * commentCounts.GetValueOrDefault(pid);

        IEnumerable<Post> ordered = posts;

        if (string.Equals(filter, "following", StringComparison.OrdinalIgnoreCase) && viewerUserId.HasValue)
        {
            var following = (await _follows.GetFollowedUserIdsAsync(viewerUserId.Value, cancellationToken)).ToHashSet();
            ordered = posts.Where(p => following.Contains(p.UserId))
                .OrderByDescending(p => Score(p.Id));
        }
        else if (string.Equals(filter, "forYou", StringComparison.OrdinalIgnoreCase) && viewerUserId.HasValue)
        {
            var mine = (await _memberships.GetActiveCommunityIdsForUserAsync(viewerUserId.Value, cancellationToken)).ToHashSet();

            bool InJoinedCommunity(Post p) =>
                p.PublicationContext == PostPublicationContext.Community
                && p.CommunityId.HasValue
                && mine.Contains(p.CommunityId.Value);

            bool InOtherCommunity(Post p) =>
                p.PublicationContext == PostPublicationContext.Community
                && p.CommunityId.HasValue
                && !mine.Contains(p.CommunityId.Value);

            var fromJoined = posts.Where(InJoinedCommunity).OrderByDescending(p => Score(p.Id)).ToList();
            var discover = posts.Where(InOtherCommunity).OrderByDescending(p => Score(p.Id)).ToList();
            var profilePosts = posts.Where(p => p.PublicationContext == PostPublicationContext.Profile)
                .OrderByDescending(p => Score(p.Id))
                .ToList();
            var seen = new HashSet<int>();
            var merged = new List<Post>();
            foreach (var p in fromJoined.Concat(discover).Concat(profilePosts))
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
        var items = await _enrichment.ToPostDtosAsync(slice, viewerUserId, cancellationToken);

        return new PaginatedResponseDto<PostResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        };
    }
}
