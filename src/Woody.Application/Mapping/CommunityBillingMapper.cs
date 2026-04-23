using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Subscription;

namespace Woody.Application.Mapping;

public static class CommunityBillingMapper
{
    public static CommunityBillingStateDto ToBillingStateDto(CommunitySubscription? subscription, DateTime utcNow)
    {
        var billing = ToApiPlan(subscription?.Plan ?? CommunityPlan.Free);
        var effective = CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow)
            ? "premium"
            : "free";
        return new CommunityBillingStateDto
        {
            BillingPlan = billing,
            EffectivePlan = effective,
            Status = SubscriptionDtoMapper.ToApiStatus(subscription?.Status ?? SubscriptionStatus.Active),
            PlanCode = subscription?.PlanCode,
            CurrentPeriodEnd = subscription?.CurrentPeriodEnd.HasValue == true
                ? EntityMappers.Iso(subscription.CurrentPeriodEnd!.Value)
                : null,
            CancelAtPeriodEnd = subscription?.CancelAtPeriodEnd ?? false,
            ProviderCustomerId = subscription?.ProviderCustomerId,
            ProviderSubscriptionId = subscription?.ProviderSubscriptionId
        };
    }

    public static string ToApiPlan(CommunityPlan plan) =>
        plan == CommunityPlan.Premium ? "premium" : "free";

    /// <summary>Resumo para embutir em previews (feed, post) — plano efetivo para gates.</summary>
    public static string ToEffectiveApiPlan(CommunitySubscription? subscription, DateTime utcNow) =>
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow) ? "premium" : "free";
}
