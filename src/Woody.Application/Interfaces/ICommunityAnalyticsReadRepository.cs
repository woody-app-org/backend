using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public interface ICommunityAnalyticsReadRepository
{
    Task<int> CountActiveMembershipsJoinedBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);

    Task<int> CountPostsPublishedBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);

    Task<int> CountCommentsOnCommunityPostsBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);

    Task<int> CountLikesOnCommunityPostsBetweenAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommunityTopPostAnalyticsRow>> GetTopCommunityPostsByScoreAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommunityTagCountRow>> GetTopPostTagsInPeriodAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<DateOnly, int>> CountPostsPerDayUtcAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<DateOnly, int>> CountCommentsPerDayUtcAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<DateOnly, int>> CountNewMembersPerDayUtcAsync(
        int communityId,
        DateTime fromUtcInclusive,
        DateTime toUtcInclusive,
        CancellationToken cancellationToken = default);
}
