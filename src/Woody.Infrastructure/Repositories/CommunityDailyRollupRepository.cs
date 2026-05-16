using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommunityDailyRollupRepository : ICommunityDailyRollupRepository
{
    private readonly WoodyDbContext _db;

    public CommunityDailyRollupRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task IncrementPageViewAsync(int communityId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime());
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO community_daily_rollups (community_id, day_utc, page_views, member_leaves)
             VALUES ({communityId}, {day}, 1, 0)
             ON CONFLICT (community_id, day_utc)
             DO UPDATE SET page_views = community_daily_rollups.page_views + 1;
             """,
            cancellationToken);
    }

    public Task IncrementMemberLeaveAsync(int communityId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime());
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO community_daily_rollups (community_id, day_utc, page_views, member_leaves)
             VALUES ({communityId}, {day}, 0, 1)
             ON CONFLICT (community_id, day_utc)
             DO UPDATE SET member_leaves = community_daily_rollups.member_leaves + 1;
             """,
            cancellationToken);
    }

    public async Task<int> SumPageViewsBetweenAsync(
        int communityId,
        DateOnly fromDayInclusive,
        DateOnly toDayInclusive,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommunityDailyRollups.AsNoTracking()
            .Where(r => r.CommunityId == communityId && r.DayUtc >= fromDayInclusive && r.DayUtc <= toDayInclusive)
            .SumAsync(r => r.PageViews, cancellationToken);
    }

    public async Task<int> SumMemberLeavesBetweenAsync(
        int communityId,
        DateOnly fromDayInclusive,
        DateOnly toDayInclusive,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommunityDailyRollups.AsNoTracking()
            .Where(r => r.CommunityId == communityId && r.DayUtc >= fromDayInclusive && r.DayUtc <= toDayInclusive)
            .SumAsync(r => r.MemberLeaves, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateOnly, (int PageViews, int MemberLeaves)>> GetRollupsBetweenAsync(
        int communityId,
        DateOnly fromDayInclusive,
        DateOnly toDayInclusive,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.CommunityDailyRollups.AsNoTracking()
            .Where(r => r.CommunityId == communityId && r.DayUtc >= fromDayInclusive && r.DayUtc <= toDayInclusive)
            .Select(r => new { r.DayUtc, r.PageViews, r.MemberLeaves })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.DayUtc, x => (x.PageViews, x.MemberLeaves));
    }
}
