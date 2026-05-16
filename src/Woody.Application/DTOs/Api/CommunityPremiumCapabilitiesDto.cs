namespace Woody.Application.DTOs.Api;

/// <summary>
/// Capacidades premium ao nível do <b>espaço</b> para a utilizadora autenticada (paridade com <see cref="Services.ICommunityPremiumEntitlementService"/>).
/// <see cref="IsStaffForPremiumTools"/> (papel na comunidade) e <see cref="CommunityPremiumActive"/> (plano da comunidade) são independentes;
/// os <c>Can*</c> exigem ambos. Novos flags premium do espaço podem acrescentar-se aqui sem misturar com assinatura de utilizadora.
/// </summary>
public sealed class CommunityPremiumCapabilitiesDto
{
    /// <summary>Owner ou admin com membership activa nesta comunidade (independente do plano do espaço ou do Woody Pro).</summary>
    public bool IsStaffForPremiumTools { get; set; }

    /// <summary>Benefícios premium do <i>espaço</i> activos (Stripe / <c>CommunitySubscription</c>), independentemente de quem consulta.</summary>
    public bool CommunityPremiumActive { get; set; }

    /// <summary>Staff + espaço premium (métricas / painel).</summary>
    public bool CanAccessCommunityAnalytics { get; set; }

    /// <summary>Staff + espaço premium (impulsionamento de posts da comunidade).</summary>
    public bool CanBoostCommunityPosts { get; set; }
}
