using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
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
    private readonly ICommunityPostBoostRepository _communityPostBoosts;
    private readonly IUserRelationshipVisibilityService _visibility;

    public FeedService(
        IPostRepository posts,
        IFollowRepository follows,
        ICommunityMembershipRepository memberships,
        ILikeRepository likes,
        ICommentRepository comments,
        IPostEnrichmentService enrichment,
        ICommunityPostBoostRepository communityPostBoosts,
        IUserRelationshipVisibilityService visibility)
    {
        _posts = posts;
        _follows = follows;
        _memberships = memberships;
        _likes = likes;
        _comments = comments;
        _enrichment = enrichment;
        _communityPostBoosts = communityPostBoosts;
        _visibility = visibility;
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

        var mode = NormalizeFeedFilter(filter);

        var candidates = await _posts.ListNonDeletedVisibleFeedCandidatesAsync(viewerUserId, cancellationToken);
        if (candidates.Count == 0)
            return EmptyPage(page, pageSize);

        if (viewerUserId.HasValue)
        {
            var hiddenIds = await _visibility.GetHiddenUserIdsForViewerAsync(viewerUserId.Value, cancellationToken);
            if (hiddenIds.Count > 0)
                candidates = candidates.Where(c => !hiddenIds.Contains(c.UserId)).ToList();
        }

        if (candidates.Count == 0)
            return EmptyPage(page, pageSize);

        HashSet<int>? followed = null;
        HashSet<int>? mine = null;
        if (viewerUserId.HasValue)
        {
            followed = (await _follows.GetFollowedUserIdsAsync(viewerUserId.Value, cancellationToken)).ToHashSet();
            mine = (await _memberships.GetActiveCommunityIdsForUserAsync(viewerUserId.Value, cancellationToken))
                .ToHashSet();
        }

        var filtered = mode switch
        {
            FeedMode.Following when !viewerUserId.HasValue => new List<PostFeedCandidate>(),
            FeedMode.Following => candidates.Where(p =>
                    followed!.Contains(p.UserId)
                    || (p.PublicationContext == PostPublicationContext.Community
                        && p.CommunityId.HasValue
                        && mine!.Contains(p.CommunityId.Value)))
                .ToList(),
            _ => candidates
        };

        if (filtered.Count == 0)
            return EmptyPage(page, pageSize);

        var filteredIds = filtered.Select(c => c.Id).Distinct().ToList();
        var likeCounts = await _likes.GetPostLikeCountsAsync(filteredIds, cancellationToken);
        var commentCounts = await _comments.GetActiveCommentCountsByPostIdsAsync(filteredIds, cancellationToken);
        var boostedPostIds = await _communityPostBoosts.GetActiveBoostedPostIdsAmongAsync(
            filteredIds,
            DateTime.UtcNow,
            cancellationToken);

        int BaseScore(int id) => likeCounts.GetValueOrDefault(id) + 2 * commentCounts.GetValueOrDefault(id);

        IEnumerable<int> orderedIds = mode switch
        {
            FeedMode.Trending => OrderTrending(filtered, BaseScore, boostedPostIds),
            FeedMode.ForYou => OrderForYou(filtered, BaseScore, viewerUserId, followed, mine, boostedPostIds),
            FeedMode.Following => OrderFollowing(filtered, BaseScore, boostedPostIds),
            _ => OrderTrending(filtered, BaseScore, boostedPostIds)
        };

        var ordered = orderedIds.ToList();
        var total = ordered.Count;
        var slice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var posts = await _posts.ListNonDeletedByIdsWithNavOrderedAsync(slice, cancellationToken);
        var items = await _enrichment.ToPostDtosAsync(posts, viewerUserId, cancellationToken);

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

    private static IEnumerable<int> OrderTrending(
        List<PostFeedCandidate> rows,
        Func<int, int> baseScore,
        HashSet<int> boostedPostIds)
    {
        return rows
            .OrderByDescending(c => boostedPostIds.Contains(c.Id) ? 1 : 0)
            .ThenByDescending(c => baseScore(c.Id))
            .ThenByDescending(c => c.CommunityMemberCountSnapshot)
            .ThenByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Select(c => c.Id);
    }

    /// <summary>
    /// Ordem cronológica (mais recentes primeiro), com desempate por interação.
    /// </summary>
    private static IEnumerable<int> OrderFollowing(
        List<PostFeedCandidate> rows,
        Func<int, int> baseScore,
        HashSet<int> boostedPostIds)
    {
        return rows
            .OrderByDescending(c => boostedPostIds.Contains(c.Id) ? 1 : 0)
            .ThenByDescending(c => c.CreatedAt)
            .ThenByDescending(c => baseScore(c.Id))
            .ThenBy(c => c.Id)
            .Select(c => c.Id);
    }

    /// <summary>
    /// Exploratório: pondera interação, proximidade social e um misto estável por utilizadora.
    /// </summary>
    private static IEnumerable<int> OrderForYou(
        List<PostFeedCandidate> rows,
        Func<int, int> baseScore,
        int? viewerUserId,
        HashSet<int>? followed,
        HashSet<int>? mine,
        HashSet<int> boostedPostIds)
    {
        return rows
            .Select(c => (c, Rank: ForYouRank(c, baseScore(c.Id), viewerUserId, followed, mine, boostedPostIds)))
            .OrderByDescending(x => x.Rank)
            .ThenByDescending(x => x.c.CreatedAt)
            .ThenBy(x => x.c.Id)
            .Select(x => x.c.Id);
    }

    private static double ForYouRank(
        PostFeedCandidate c,
        int score,
        int? viewerUserId,
        HashSet<int>? followed,
        HashSet<int>? mine,
        HashSet<int> boostedPostIds)
    {
        var w = 1.0;
        if (followed != null && followed.Contains(c.UserId))
            w *= 1.45;
        if (mine != null && c.CommunityId.HasValue && mine.Contains(c.CommunityId.Value))
            w *= 1.22;
        if (boostedPostIds.Contains(c.Id))
            w *= 1.85;

        var ageDays = (DateTime.UtcNow - c.CreatedAt).TotalDays;
        if (ageDays <= 7)
            w *= 1.18;
        else if (ageDays <= 30)
            w *= 1.06;

        var mix = StableMix(viewerUserId, c.Id) % 4096;
        return score * w * 10_000 + mix;
    }

    private static int StableMix(int? viewerUserId, int postId)
    {
        unchecked
        {
            var seed = viewerUserId ?? -7919;
            return (seed * 397) ^ (postId * 7919) ^ (seed << 5);
        }
    }

    private static FeedMode NormalizeFeedFilter(string filter)
    {
        var f = (filter ?? string.Empty).Trim().ToLowerInvariant();
        if (f is "foryou" or "para-voce" or "para_voce")
            return FeedMode.ForYou;
        if (f is "following" or "seguindo")
            return FeedMode.Following;
        if (f is "trending" or "em-alta" or "emalta" or "hot")
            return FeedMode.Trending;
        return FeedMode.Trending;
    }

    private static PaginatedResponseDto<PostResponseDto> EmptyPage(int page, int pageSize) =>
        new()
        {
            Items = new List<PostResponseDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            HasNextPage = false,
            HasPreviousPage = page > 1
        };

    private enum FeedMode
    {
        Trending,
        ForYou,
        Following
    }
}
