using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface ICommunityMembershipRepository
{
    Task<List<string>> GetActiveCommunityIdsAsStringsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<CommunityMembership>> ListActiveWithCommunityAndTagsByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<CommunityMembership?> GetForUserAndCommunityAsync(int userId, int communityId, CancellationToken cancellationToken = default);
    Task<CommunityMembership?> GetActiveForUserAndCommunityNoTrackingAsync(int userId, int communityId, CancellationToken cancellationToken = default);
    Task<(List<CommunityMembership> Rows, int Total)> ListActiveMembersPagedOrderedAsync(
        int communityId,
        int page,
        int pageSize,
        IReadOnlyCollection<int>? excludeUserIds = null,
        CancellationToken cancellationToken = default);
    Task<int> CountActiveInCommunityAsync(int communityId, CancellationToken cancellationToken = default);

    /// <summary>Donas e administradoras ativas da comunidade (para notificações de pedido de entrada).</summary>
    Task<List<int>> ListActiveModeratorUserIdsForCommunityAsync(int communityId, CancellationToken cancellationToken = default);
    void Add(CommunityMembership membership);
    void Remove(CommunityMembership membership);
    Task<List<int>> GetActiveCommunityIdsForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForUserAndCommunityAsync(int userId, int communityId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
