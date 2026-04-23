using Woody.Domain.Entities;

namespace Woody.Domain.Subscription;

/// <summary>
/// Combina estado de assinatura da comunidade com papel da utilizadora (owner/admin) para recursos de administração/crescimento.
/// O plano da comunidade não substitui o papel: moderador sem premium não acede a extras premium; membro premium-only não acede a ferramentas de staff.
/// </summary>
public static class CommunityPremiumFeatureGate
{
    /// <summary>Benefícios ao nível do espaço (independentemente de quem consulta).</summary>
    public static bool CanAccessCommunityPremiumFeatures(CommunitySubscription? subscription, DateTime utcNow) =>
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    /// <summary>Analytics e métricas de comunidade: requer comunidade premium e papel de moderação.</summary>
    public static bool CanAccessCommunityAnalytics(CommunityMembership? membership, CommunitySubscription? subscription,
        DateTime utcNow) =>
        IsCommunityStaff(membership?.Role) &&
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    /// <summary>Impulsionar post no contexto da comunidade: staff + comunidade premium (base para futura feature).</summary>
    public static bool CanBoostCommunityPost(CommunityMembership? membership, CommunitySubscription? subscription,
        DateTime utcNow) =>
        IsCommunityStaff(membership?.Role) &&
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    private static bool IsCommunityStaff(string? role) =>
        string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
}
