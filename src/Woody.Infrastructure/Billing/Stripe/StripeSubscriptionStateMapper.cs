using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Domain.Entities.Enum;

namespace Woody.Infrastructure.Billing.StripePayments;

internal static class StripeSubscriptionStateMapper
{
    public static BillingSubscriptionSnapshot ToSnapshot(global::Stripe.Subscription subscription, BillingOptions options)
    {
        var primaryItem = subscription.Items?.Data?.FirstOrDefault();
        var priceId = primaryItem?.Price?.Id;
        var (plan, planCode) = ResolvePlan(priceId, options);
        var status = MapStatus(subscription);
        var periodStart = ToUtcDateTime(primaryItem?.CurrentPeriodStart);
        var periodEnd = ToUtcDateTime(primaryItem?.CurrentPeriodEnd);

        return new BillingSubscriptionSnapshot(
            subscription.CustomerId ?? string.Empty,
            subscription.Id,
            plan,
            planCode,
            status,
            periodStart,
            periodEnd,
            subscription.CancelAtPeriodEnd,
            priceId);
    }

    private static (SubscriptionPlan Plan, string? PlanCode) ResolvePlan(string? priceId, BillingOptions options)
    {
        var proMonthly = options.Stripe?.PriceIds?.ProMonthly;
        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(proMonthly) &&
            string.Equals(priceId, proMonthly, StringComparison.Ordinal))
            return (SubscriptionPlan.Pro, BillingPlanCodes.ProMonthly);

        if (!string.IsNullOrEmpty(priceId))
            return (SubscriptionPlan.Pro, null);

        return (SubscriptionPlan.Free, BillingPlanCodes.Free);
    }

    private static DateTime? ToUtcDateTime(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;

    private static SubscriptionStatus MapStatus(global::Stripe.Subscription subscription)
    {
        if (subscription.Status == "active" && subscription.CancelAtPeriodEnd)
            return SubscriptionStatus.Canceling;

        return subscription.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.PastDue,
            "incomplete" => SubscriptionStatus.PastDue,
            "incomplete_expired" => SubscriptionStatus.Expired,
            "paused" => SubscriptionStatus.PastDue,
            _ => SubscriptionStatus.Expired
        };
    }
}
