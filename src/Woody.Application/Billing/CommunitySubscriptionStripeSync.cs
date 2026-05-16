using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Billing;

/// <summary>Alinha <see cref="CommunitySubscription"/> com o estado normalizado do Stripe.</summary>
public static class CommunitySubscriptionStripeSync
{
    public static void ApplyGatewaySnapshot(CommunitySubscription subscription,
        CommunityBillingSubscriptionSnapshot snapshot, DateTime utcNow)
    {
        if (!string.IsNullOrEmpty(snapshot.ProviderCustomerId))
            subscription.ProviderCustomerId = snapshot.ProviderCustomerId;
        if (!string.IsNullOrEmpty(snapshot.ProviderSubscriptionId))
            subscription.ProviderSubscriptionId = snapshot.ProviderSubscriptionId;

        subscription.BillingProvider = BillingProvider.Stripe;
        subscription.Plan = snapshot.Plan;
        subscription.PlanCode = snapshot.PlanCode ?? subscription.PlanCode;
        subscription.Status = snapshot.Status;
        subscription.CurrentPeriodStart = snapshot.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = snapshot.CurrentPeriodEnd;
        subscription.CancelAtPeriodEnd = snapshot.CancelAtPeriodEnd;
        subscription.UpdatedAt = utcNow;
    }

    public static void ApplySubscriptionRemoved(CommunitySubscription subscription, DateTime utcNow)
    {
        subscription.ProviderSubscriptionId = null;
        subscription.Plan = CommunityPlan.Free;
        subscription.PlanCode = CommunityBillingPlanCodes.Free;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = null;
        subscription.CurrentPeriodEnd = null;
        subscription.CancelAtPeriodEnd = false;
        subscription.BillingProvider = string.IsNullOrEmpty(subscription.ProviderCustomerId)
            ? BillingProvider.None
            : BillingProvider.Stripe;
        subscription.UpdatedAt = utcNow;
    }
}
