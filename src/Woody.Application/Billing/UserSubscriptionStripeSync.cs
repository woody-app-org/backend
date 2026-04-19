using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Billing;

/// <summary>Alinha <see cref="UserSubscription"/> com o estado normalizado do Stripe (fonte de verdade externa).</summary>
public static class UserSubscriptionStripeSync
{
    public static void ApplyGatewaySnapshot(UserSubscription subscription, BillingSubscriptionSnapshot snapshot,
        DateTime utcNow)
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

    /// <summary>Após <c>customer.subscription.deleted</c> — remove subscrição ativa e repõe nível Free.</summary>
    public static void ApplySubscriptionRemoved(UserSubscription subscription, DateTime utcNow)
    {
        subscription.ProviderSubscriptionId = null;
        subscription.Plan = SubscriptionPlan.Free;
        subscription.PlanCode = BillingPlanCodes.Free;
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
