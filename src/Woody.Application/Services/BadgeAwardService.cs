using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;

namespace Woody.Application.Services;

public class BadgeAwardService : IBadgeAwardService
{
    private readonly IBadgeRepository _badges;

    public BadgeAwardService(IBadgeRepository badges)
    {
        _badges = badges;
    }

    public async Task<List<UserBadgeDto>> GetUserBadgesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var rows = await _badges.GetActiveUserBadgesAsync(userId, cancellationToken);
        return rows.Select(EntityMappers.ToUserBadgeDto).ToList();
    }

    public async Task<BadgeAwardOutcome> AwardBadgeAsync(
        int userId,
        string badgeSlug,
        DateTime? earnedAt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(badgeSlug))
            return BadgeAwardOutcome.BadgeNotFound;

        var badge = await _badges.GetBySlugAsync(badgeSlug.Trim(), cancellationToken);
        if (badge == null)
            return BadgeAwardOutcome.BadgeNotFound;

        if (!badge.IsActive)
            return BadgeAwardOutcome.BadgeInactive;

        if (await _badges.UserHasBadgeAsync(userId, badge.Slug, cancellationToken))
            return BadgeAwardOutcome.AlreadyOwned;

        var at = earnedAt ?? DateTime.UtcNow;
        var inserted = await _badges.TryAddUserBadgeAsync(userId, badge.Id, at, cancellationToken);
        return inserted ? BadgeAwardOutcome.Awarded : BadgeAwardOutcome.AlreadyOwned;
    }

    public Task<bool> UserHasBadgeAsync(int userId, string badgeSlug, CancellationToken cancellationToken = default) =>
        _badges.UserHasBadgeAsync(userId, badgeSlug, cancellationToken);
}
