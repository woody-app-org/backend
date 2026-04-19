using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Assinatura/plano da utilizadora (1:1 com <see cref="User"/>). Fonte de verdade para benefícios Pro e sincronização com o gateway.
/// </summary>
public class UserSubscription
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Nível de produto (Free/Pro) derivado do gateway ou padrão interno.</summary>
    public SubscriptionPlan Plan { get; set; }

    /// <summary>Ciclo de vida da assinatura paga; para Free usar <see cref="SubscriptionStatus.Active"/>.</summary>
    public SubscriptionStatus Status { get; set; }

    /// <summary>Código de catálogo comercial (ex.: free, pro_monthly). Complementa <see cref="Plan"/> quando há vários preços.</summary>
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
