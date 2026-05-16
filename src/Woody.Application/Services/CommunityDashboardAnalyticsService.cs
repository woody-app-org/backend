using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;

namespace Woody.Application.Services;

public class CommunityDashboardAnalyticsService : ICommunityDashboardAnalyticsService
{
    private readonly ICommunityRepository _communities;
    private readonly IPostRepository _posts;
    private readonly ICommunityDailyRollupRepository _rollups;
    private readonly ICommunityAnalyticsReadRepository _analytics;

    public CommunityDashboardAnalyticsService(
        ICommunityRepository communities,
        IPostRepository posts,
        ICommunityDailyRollupRepository rollups,
        ICommunityAnalyticsReadRepository analytics)
    {
        _communities = communities;
        _posts = posts;
        _rollups = rollups;
        _analytics = analytics;
    }

    public async Task<CommunityPremiumDashboardAnalyticsDto> BuildDashboardAsync(
        int communityId,
        string? slug,
        int periodDays,
        CancellationToken cancellationToken = default)
    {
        periodDays = Math.Clamp(periodDays, 7, 365);
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-periodDays);
        var span = toUtc - fromUtc;
        var prevToUtc = fromUtc;
        var prevFromUtc = fromUtc - span;

        var fromDay = DateOnly.FromDateTime(fromUtc);
        var toDay = DateOnly.FromDateTime(toUtc);
        var prevFromDay = DateOnly.FromDateTime(prevFromUtc);
        var prevToDay = DateOnly.FromDateTime(prevToUtc);

        var community = await _communities.GetByIdWithTagsNoTrackingAsync(communityId, cancellationToken)
                        ?? throw new InvalidOperationException("Community not found.");

        var totalPosts = await _posts.CountNonDeletedCommunityPostsAsync(communityId, cancellationToken);

        var current = await BuildBucketAsync(communityId, fromUtc, toUtc, fromDay, toDay, cancellationToken);
        var previous = await BuildBucketAsync(communityId, prevFromUtc, prevToUtc, prevFromDay, prevToDay, cancellationToken);

        var interactions = current.LikesOnPosts + current.CommentsPosted;
        var engagement = new CommunityEngagementSummaryDto
        {
            AverageInteractionsPerPost = current.PostsPublished == 0
                ? 0
                : Math.Round(interactions / (double)current.PostsPublished, 2)
        };

        var topPostsRows = await _analytics.GetTopCommunityPostsByScoreAsync(
            communityId, fromUtc, toUtc, 8, cancellationToken);
        var topPosts = topPostsRows.Select(r => new CommunityTopPostAnalyticsDto
        {
            PostId = r.PostId.ToString(),
            ContentPreview = r.ContentPreview,
            CreatedAtUtc = r.CreatedAtUtc,
            LikesCount = r.LikesCount,
            CommentsCount = r.CommentsCount,
            Score = r.LikesCount + r.CommentsCount,
            AuthorUsername = r.AuthorUsername
        }).ToList();

        var topTagRows = await _analytics.GetTopPostTagsInPeriodAsync(communityId, fromUtc, toUtc, 12, cancellationToken);
        var topTags = topTagRows.Select(t => new CommunityTagCountDto { Tag = t.Tag, Count = t.Count }).ToList();

        var postsByDay = await _analytics.CountPostsPerDayUtcAsync(communityId, fromUtc, toUtc, cancellationToken);
        var commentsByDay = await _analytics.CountCommentsPerDayUtcAsync(communityId, fromUtc, toUtc, cancellationToken);
        var membersByDay = await _analytics.CountNewMembersPerDayUtcAsync(communityId, fromUtc, toUtc, cancellationToken);
        var rollupByDay = await _rollups.GetRollupsBetweenAsync(communityId, fromDay, toDay, cancellationToken);

        var daily = new List<CommunityDailyActivityPointDto>();
        for (var d = fromDay; d <= toDay; d = d.AddDays(1))
        {
            rollupByDay.TryGetValue(d, out var roll);
            daily.Add(new CommunityDailyActivityPointDto
            {
                DayUtc = d,
                Posts = postsByDay.GetValueOrDefault(d),
                Comments = commentsByDay.GetValueOrDefault(d),
                PageViews = roll.PageViews,
                MemberLeaves = roll.MemberLeaves,
                NewMembers = membersByDay.GetValueOrDefault(d)
            });
        }

        return new CommunityPremiumDashboardAnalyticsDto
        {
            CommunityId = communityId.ToString(),
            Slug = slug ?? community.Slug,
            PeriodDays = periodDays,
            PeriodStartUtc = fromUtc,
            PeriodEndUtc = toUtc,
            PreviousPeriodStartUtc = prevFromUtc,
            PreviousPeriodEndUtc = prevToUtc,
            MemberCount = community.MemberCount,
            TotalPosts = totalPosts,
            Headline = "Painel da comunidade",
            Note =
                "Visitas contam carregamentos da página pública da comunidade (API). Saídas são agregadas sem identificar quem saiu. Dados históricos de visitas/saídas existem apenas após esta versão.",
            Current = current,
            Previous = previous,
            Engagement = engagement,
            TopPosts = topPosts,
            TopTags = topTags,
            DailyActivity = daily
        };
    }

    private async Task<CommunityAnalyticsPeriodBucketDto> BuildBucketAsync(
        int communityId,
        DateTime fromUtc,
        DateTime toUtc,
        DateOnly fromDay,
        DateOnly toDay,
        CancellationToken cancellationToken)
    {
        var joins = await _analytics.CountActiveMembershipsJoinedBetweenAsync(
            communityId, fromUtc, toUtc, cancellationToken);
        var leaves = await _rollups.SumMemberLeavesBetweenAsync(communityId, fromDay, toDay, cancellationToken);
        var views = await _rollups.SumPageViewsBetweenAsync(communityId, fromDay, toDay, cancellationToken);
        var posts = await _analytics.CountPostsPublishedBetweenAsync(communityId, fromUtc, toUtc, cancellationToken);
        var comments = await _analytics.CountCommentsOnCommunityPostsBetweenAsync(communityId, fromUtc, toUtc, cancellationToken);
        var likes = await _analytics.CountLikesOnCommunityPostsBetweenAsync(communityId, fromUtc, toUtc, cancellationToken);

        return new CommunityAnalyticsPeriodBucketDto
        {
            NewMembersJoined = joins,
            MemberLeavesRecorded = leaves,
            PageViews = views,
            PostsPublished = posts,
            CommentsPosted = comments,
            LikesOnPosts = likes
        };
    }
}
