using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Subscription;

/// <summary>
/// Regras de elegibilidade a partir do estado persistido da assinatura da comunidade (sem misturar com membership/role).
/// </summary>
public static class CommunitySubscriptionEntitlement
{
    public static bool HasActivePremiumBenefits(CommunitySubscription? subscription, DateTime utcNow)
    {
        if (subscription is null)
            return false;
        if (subscription.Plan != CommunityPlan.Premium)
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

    private static bool IsPeriodEnded(CommunitySubscription subscription, DateTime utcNow) =>
        subscription.CurrentPeriodEnd.HasValue && subscription.CurrentPeriodEnd.Value <= utcNow;
}
