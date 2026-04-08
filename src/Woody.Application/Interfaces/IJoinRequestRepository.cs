using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IJoinRequestRepository
{
    Task<List<JoinRequest>> ListPendingWithUserForCommunityAsync(int communityId, CancellationToken cancellationToken = default);
    Task<JoinRequest?> GetWithCommunityTrackedAsync(int joinRequestId, CancellationToken cancellationToken = default);
    Task<JoinRequest?> GetTrackedAsync(int joinRequestId, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingAsync(int communityId, int userId, CancellationToken cancellationToken = default);
    void Add(JoinRequest request);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
