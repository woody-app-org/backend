using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Subscription;

namespace Woody.Application.Services;

/// <summary>
/// Regras de produto ligadas ao <see cref="UserSubscription"/> (plano Pro, features premium).
/// Quem pode <em>moderar uma comunidade</em> continua em <see cref="CommunityPermissionService"/> (papéis owner/admin na membership).
/// </summary>
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
