using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Subscription;

namespace Woody.Application.Services;

public class UserEntitlementService : IUserEntitlementService
{
    private readonly IUserSubscriptionRepository _subscriptions;

    public UserEntitlementService(IUserSubscriptionRepository subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public Task<UserSubscription?> GetSubscriptionAsync(int userId, CancellationToken cancellationToken = default) =>
        _subscriptions.GetByUserIdNoTrackingAsync(userId, cancellationToken);

    public async Task<bool> HasActiveProBenefitsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var s = await _subscriptions.GetByUserIdNoTrackingAsync(userId, cancellationToken);
        return SubscriptionEntitlement.HasActiveProBenefits(s, DateTime.UtcNow);
    }

    public async Task<bool> CanCreateCommunityAsync(int userId, CancellationToken cancellationToken = default)
    {
        var s = await _subscriptions.GetByUserIdNoTrackingAsync(userId, cancellationToken);
        return SubscriptionEntitlement.CanCreateCommunity(s, DateTime.UtcNow);
    }

    public async Task<bool> CanAccessPremiumFeatureAsync(int userId, CancellationToken cancellationToken = default)
    {
        var s = await _subscriptions.GetByUserIdNoTrackingAsync(userId, cancellationToken);
        return SubscriptionEntitlement.CanAccessPremiumFeature(s, DateTime.UtcNow);
    }
}
