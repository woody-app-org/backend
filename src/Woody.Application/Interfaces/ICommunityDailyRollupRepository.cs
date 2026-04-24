namespace Woody.Application.Interfaces;

public interface ICommunityDailyRollupRepository
{
    Task IncrementPageViewAsync(int communityId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task IncrementMemberLeaveAsync(int communityId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<int> SumPageViewsBetweenAsync(
        int communityId,
        DateOnly fromDayInclusive,
        DateOnly toDayInclusive,
        CancellationToken cancellationToken = default);

    Task<int> SumMemberLeavesBetweenAsync(
        int communityId,
        DateOnly fromDayInclusive,
        DateOnly toDayInclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<DateOnly, (int PageViews, int MemberLeaves)>> GetRollupsBetweenAsync(
        int communityId,
        DateOnly fromDayInclusive,
        DateOnly toDayInclusive,
        CancellationToken cancellationToken = default);
}
