using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

/// <summary>Persistência 1:1 de <see cref="CommunitySubscription"/>.</summary>
public interface ICommunitySubscriptionRepository
{
    Task<CommunitySubscription?> GetByCommunityIdNoTrackingAsync(int communityId,
        CancellationToken cancellationToken = default);

    Task<CommunitySubscription?> GetByCommunityIdTrackedAsync(int communityId,
        CancellationToken cancellationToken = default);

    Task<CommunitySubscription?> GetByProviderSubscriptionIdTrackedAsync(string providerSubscriptionId,
        CancellationToken cancellationToken = default);

    Task<CommunitySubscription?> GetByProviderCustomerIdTrackedAsync(string providerCustomerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CommunitySubscription subscription, CancellationToken cancellationToken = default);
    void Update(CommunitySubscription subscription);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
