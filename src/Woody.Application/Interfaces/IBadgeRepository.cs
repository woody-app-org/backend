using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IBadgeRepository
{
    Task<Badge?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<List<UserBadge>> GetActiveUserBadgesAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UserHasBadgeAsync(int userId, string badgeSlug, CancellationToken cancellationToken = default);

    /// <summary>Insere a conquista; retorna <c>true</c> se nova, <c>false</c> se já existia ou conflito de duplicidade.</summary>
    Task<bool> TryAddUserBadgeAsync(int userId, int badgeId, DateTime earnedAt, CancellationToken cancellationToken = default);
}
