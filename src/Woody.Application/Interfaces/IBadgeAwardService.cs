using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public enum BadgeAwardOutcome
{
    Awarded,
    AlreadyOwned,
    BadgeNotFound,
    BadgeInactive
}

public interface IBadgeAwardService
{
    Task<List<UserBadgeDto>> GetUserBadgesAsync(int userId, CancellationToken cancellationToken = default);

    Task<BadgeAwardOutcome> AwardBadgeAsync(
        int userId,
        string badgeSlug,
        DateTime? earnedAt = null,
        CancellationToken cancellationToken = default);

    Task<bool> UserHasBadgeAsync(int userId, string badgeSlug, CancellationToken cancellationToken = default);
}
