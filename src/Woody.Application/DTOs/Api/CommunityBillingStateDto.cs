namespace Woody.Application.DTOs.Api;

/// <summary>Estado de cobrança da comunidade (separado do papel da utilizadora na membership).</summary>
public sealed class CommunityBillingStateDto
{
    /// <summary>Plano persistido no catálogo (<c>free</c> | <c>premium</c>).</summary>
    public string BillingPlan { get; set; } = "free";

    /// <summary>Plano efetivo após regras de período/status (para feature gating).</summary>
    public string EffectivePlan { get; set; } = "free";

    public string Status { get; set; } = "active";
    public string? PlanCode { get; set; }
    public string? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
}
