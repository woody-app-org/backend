using Woody.Domain.Entities.Enum;

namespace Woody.Application.Billing;

/// <summary>
/// Vista normalizada de uma assinatura no gateway, desacoplada do SDK Stripe.
/// Usada por webhooks/checkout (fases seguintes) para alinhar a BD com o estado remoto.
/// </summary>
public sealed record BillingSubscriptionSnapshot(
    string ProviderCustomerId,
    string ProviderSubscriptionId,
    SubscriptionPlan Plan,
    string? PlanCode,
    SubscriptionStatus Status,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    string? PrimaryPriceId);
