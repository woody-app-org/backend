using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface ICommunityPostBoostRepository
{
    Task CancelActiveForPostAsync(int postId, DateTime utcNow, CancellationToken cancellationToken = default);

    void Add(CommunityPostBoost boost);

    Task<List<CommunityPostBoost>> ListActiveForCommunityAsync(
        int communityId,
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken = default);

    Task<CommunityPostBoost?> GetActiveForPostInCommunityAsync(
        int communityId,
        int postId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetActiveBoostedPostIdsAmongAsync(
        IReadOnlyList<int> postIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, DateTime>> GetActiveBoostEndsByPostIdsAsync(
        IReadOnlyList<int> postIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
