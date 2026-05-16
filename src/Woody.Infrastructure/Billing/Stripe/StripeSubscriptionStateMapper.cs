using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Domain.Entities.Enum;

namespace Woody.Infrastructure.Billing.StripePayments;

internal static class StripeSubscriptionStateMapper
{
    public static BillingSubscriptionReadResult ToReadResult(global::Stripe.Subscription subscription,
        BillingOptions options)
    {
        int? communityHint = null;
        if (subscription.Metadata != null &&
            subscription.Metadata.TryGetValue(StripeBillingMetadataKeys.WoodyCommunityId, out var cidRaw) &&
            int.TryParse(cidRaw, out var cid))
            communityHint = cid;

        if (IsCommunityStripeSubscription(subscription, options))
            return new BillingSubscriptionReadResult(null, ToCommunitySnapshot(subscription, options), communityHint);

        return new BillingSubscriptionReadResult(ToSnapshot(subscription, options), null, communityHint);
    }

    private static bool IsCommunityStripeSubscription(global::Stripe.Subscription subscription, BillingOptions options)
    {
        if (subscription.Metadata != null &&
            subscription.Metadata.TryGetValue(StripeBillingMetadataKeys.WoodyBillingSubject, out var subject) &&
            string.Equals(subject, StripeBillingMetadataKeys.SubjectCommunityPremium, StringComparison.Ordinal))
            return true;

        var primaryItem = subscription.Items?.Data?.FirstOrDefault();
        var priceId = primaryItem?.Price?.Id;
        return MatchesCommunityPremiumPrice(priceId, options);
    }

    private static bool MatchesCommunityPremiumPrice(string? priceId, BillingOptions options)
    {
        if (string.IsNullOrEmpty(priceId))
            return false;

        var monthly = options.Stripe?.PriceIds?.CommunityPremiumMonthly?.Trim();
        var annual = options.Stripe?.PriceIds?.CommunityPremiumAnnual?.Trim();
        if (!string.IsNullOrEmpty(monthly) && string.Equals(priceId, monthly, StringComparison.Ordinal))
            return true;
        if (!string.IsNullOrEmpty(annual) && string.Equals(priceId, annual, StringComparison.Ordinal))
            return true;
        return false;
    }

    internal static CommunityBillingSubscriptionSnapshot ToCommunitySnapshot(global::Stripe.Subscription subscription,
        BillingOptions options)
    {
        var primaryItem = subscription.Items?.Data?.FirstOrDefault();
        var priceId = primaryItem?.Price?.Id;
        var (plan, planCode) = ResolveCommunityPlan(priceId, options);
        var status = MapStatus(subscription);
        var periodStart = ToUtcDateTime(primaryItem?.CurrentPeriodStart);
        var periodEnd = ToUtcDateTime(primaryItem?.CurrentPeriodEnd);

        return new CommunityBillingSubscriptionSnapshot(
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

    private static (CommunityPlan Plan, string? PlanCode) ResolveCommunityPlan(string? priceId, BillingOptions options)
    {
        var monthly = options.Stripe?.PriceIds?.CommunityPremiumMonthly?.Trim();
        var annual = options.Stripe?.PriceIds?.CommunityPremiumAnnual?.Trim();
        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(monthly) &&
            string.Equals(priceId, monthly, StringComparison.Ordinal))
            return (CommunityPlan.Premium, CommunityBillingPlanCodes.PremiumMonthly);

        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(annual) &&
            string.Equals(priceId, annual, StringComparison.Ordinal))
            return (CommunityPlan.Premium, CommunityBillingPlanCodes.PremiumAnnual);

        if (!string.IsNullOrEmpty(priceId))
            return (CommunityPlan.Premium, null);

        return (CommunityPlan.Free, CommunityBillingPlanCodes.Free);
    }

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
        var maxMonthly = options.Stripe?.PriceIds?.MaxMonthly?.Trim();
        var maxAnnual = options.Stripe?.PriceIds?.MaxAnnual?.Trim();
        var proMonthly = options.Stripe?.PriceIds?.ProMonthly?.Trim();
        var proAnnual = options.Stripe?.PriceIds?.ProAnnual?.Trim();

        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(maxMonthly) &&
            string.Equals(priceId, maxMonthly, StringComparison.Ordinal))
            return (SubscriptionPlan.Max, BillingPlanCodes.MaxMonthly);

        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(maxAnnual) &&
            string.Equals(priceId, maxAnnual, StringComparison.Ordinal))
            return (SubscriptionPlan.Max, BillingPlanCodes.MaxAnnual);

        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(proMonthly) &&
            string.Equals(priceId, proMonthly, StringComparison.Ordinal))
            return (SubscriptionPlan.Pro, BillingPlanCodes.ProMonthly);

        if (!string.IsNullOrEmpty(priceId) && !string.IsNullOrEmpty(proAnnual) &&
            string.Equals(priceId, proAnnual, StringComparison.Ordinal))
            return (SubscriptionPlan.Pro, BillingPlanCodes.ProAnnual);

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
