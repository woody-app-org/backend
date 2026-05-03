namespace Woody.Application.DTOs;

/// <summary>
/// Estado de assinatura exposto à API (camelCase via serialização padrão ASP.NET).
/// Renovação, cancelamento e <see cref="CancelAtPeriodEnd"/> serão preenchidos pelo motor de billing/webhooks;
/// o cliente usa estes campos para UX e trata o servidor como fonte de verdade para permissões.
/// </summary>
public class UserSubscriptionStateDto
{
    /// <summary>Plano com benefícios ativos neste momento (<c>free</c> ou <c>pro</c>).</summary>
    public string EffectivePlan { get; set; } = "free";

    /// <summary>Plano de faturação persistido (<c>free</c>, <c>pro</c>, <c>max</c>); benefícios efetivos vê <see cref="EffectivePlan"/>.</summary>
    public string BillingPlan { get; set; } = "free";

    /// <summary>Código de catálogo (ex.: <c>free</c>, <c>pro_monthly</c>); alinhado com Stripe price ids na configuração.</summary>
    public string? PlanCode { get; set; }

    public string Status { get; set; } = "active";
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public bool ShowProBadge { get; set; }

    /// <summary>Indica se a sessão pode abrir o Stripe Customer Billing Portal (<c>cus_…</c> persistido).</summary>
    public bool CanOpenBillingPortal { get; set; }
}
