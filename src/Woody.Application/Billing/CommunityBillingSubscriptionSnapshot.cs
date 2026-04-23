using Woody.Domain.Entities.Enum;

namespace Woody.Application.Billing;

/// <summary>
/// Vista normalizada de uma assinatura de comunidade no Stripe (paralela a <see cref="BillingSubscriptionSnapshot"/> para utilizadora).
/// </summary>
public sealed record CommunityBillingSubscriptionSnapshot(
    string ProviderCustomerId,
    string ProviderSubscriptionId,
    CommunityPlan Plan,
    string? PlanCode,
    SubscriptionStatus Status,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    string? PrimaryPriceId);
