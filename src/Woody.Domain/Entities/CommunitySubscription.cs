using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Assinatura/plano da comunidade (1:1 com <see cref="Community"/>). Fonte de verdade para benefícios premium ao nível do espaço e futura sincronização Stripe.
/// </summary>
public class CommunitySubscription
{
    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;

    public CommunityPlan Plan { get; set; }

    /// <summary>Ciclo de vida da assinatura paga; para <see cref="CommunityPlan.Free"/> usar <see cref="SubscriptionStatus.Active"/>.</summary>
    public SubscriptionStatus Status { get; set; }

    /// <summary>Código de catálogo (ex.: community_free, community_premium_monthly).</summary>
    public string? PlanCode { get; set; }

    public BillingProvider BillingProvider { get; set; }

    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
