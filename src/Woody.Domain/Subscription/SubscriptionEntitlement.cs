using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Subscription;

/// <summary>
/// Regras centralizadas de acesso a benefícios Pro (evitar ifs espalhados em handlers).
/// </summary>
public static class SubscriptionEntitlement
{
    public static bool HasActiveProBenefits(UserSubscription? subscription, DateTime utcNow)
    {
        if (subscription is null)
            return false;
        if (subscription.Plan != SubscriptionPlan.Pro && subscription.Plan != SubscriptionPlan.Max)
            return false;

        return subscription.Status switch
        {
            SubscriptionStatus.Active or SubscriptionStatus.Trialing => !IsPeriodEnded(subscription, utcNow),
            SubscriptionStatus.PastDue => false,
            SubscriptionStatus.Canceling or SubscriptionStatus.Canceled =>
                subscription.CurrentPeriodEnd.HasValue && subscription.CurrentPeriodEnd.Value > utcNow,
            SubscriptionStatus.Expired => false,
            _ => false
        };
    }

    public static bool ShouldShowProBadge(UserSubscription? subscription, DateTime utcNow) =>
        HasActiveProBenefits(subscription, utcNow);

    public static bool CanCreateCommunity(UserSubscription? subscription, DateTime utcNow) =>
        HasActiveProBenefits(subscription, utcNow);

    public static bool CanAccessPremiumFeature(UserSubscription? subscription, DateTime utcNow) =>
        HasActiveProBenefits(subscription, utcNow);

    private static bool IsPeriodEnded(UserSubscription subscription, DateTime utcNow) =>
        subscription.CurrentPeriodEnd.HasValue && subscription.CurrentPeriodEnd.Value <= utcNow;
}
