using Woody.Application.DTOs;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Subscription;

namespace Woody.Application.Mapping;

public static class SubscriptionDtoMapper
{
    public static UserSubscriptionStateDto ToStateDto(UserSubscription? subscription, DateTime utcNow)
    {
        var billing = ToApiPlan(subscription?.Plan ?? SubscriptionPlan.Free);
        var effective = SubscriptionEntitlement.HasActiveProBenefits(subscription, utcNow) ? "pro" : "free";
        return new UserSubscriptionStateDto
        {
            EffectivePlan = effective,
            BillingPlan = billing,
            Status = ToApiStatus(subscription?.Status ?? SubscriptionStatus.Active),
            CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
            CancelAtPeriodEnd = subscription?.CancelAtPeriodEnd ?? false,
            ShowProBadge = SubscriptionEntitlement.ShouldShowProBadge(subscription, utcNow)
        };
    }

    public static string ToApiPlan(SubscriptionPlan plan) =>
        plan == SubscriptionPlan.Pro ? "pro" : "free";

    public static string ToApiStatus(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Active => "active",
        SubscriptionStatus.Trialing => "trialing",
        SubscriptionStatus.PastDue => "past_due",
        SubscriptionStatus.Canceling => "canceling",
        SubscriptionStatus.Canceled => "canceled",
        SubscriptionStatus.Expired => "expired",
        _ => "active"
    };
}
