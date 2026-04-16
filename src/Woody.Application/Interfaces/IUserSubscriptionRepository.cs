using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByUserIdNoTrackingAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetByUserIdTrackedAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);
    void Update(UserSubscription subscription);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
