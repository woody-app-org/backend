namespace Woody.Application.DTOs;

/// <summary>
/// Estado de assinatura exposto à API (camelCase via serialização padrão ASP.NET).
/// </summary>
public class UserSubscriptionStateDto
{
    /// <summary>Plano com benefícios ativos neste momento (<c>free</c> ou <c>pro</c>).</summary>
    public string EffectivePlan { get; set; } = "free";

    /// <summary>Plano persistido na base (pode ser <c>pro</c> sem benefícios se expirado/cancelado).</summary>
    public string BillingPlan { get; set; } = "free";

    public string Status { get; set; } = "active";
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public bool ShowProBadge { get; set; }
}
