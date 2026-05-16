using Woody.Domain.Entities;

namespace Woody.Domain.Subscription;

/// <summary>
/// Combina estado de assinatura <b>da comunidade</b> com papel da utilizadora (owner/admin) para recursos de administração/crescimento.
/// O plano do espaço não substitui o papel: staff sem premium não acede a extras premium; membro com espaço premium não acede a ferramentas de staff.
/// O Woody Pro da utilizadora não entra nestas funções — mantém-se em regras de utilizadora (ex.: criar comunidade).
/// Novos tiers de espaço (<see cref="Entities.Enum.CommunityPlan"/>) podem estender-se em <see cref="CommunitySubscriptionEntitlement"/> sem alterar esta matriz staff × espaço.
/// </summary>
public static class CommunityPremiumFeatureGate
{
    /// <summary>Owner ou admin ativo (papel na comunidade) — independente do plano premium.</summary>
    public static bool IsStaffForPremiumTools(string? membershipRole) => IsCommunityStaff(membershipRole);

    /// <summary>Comunidade com plano premium ativo (independente do papel da utilizadora).</summary>
    public static bool CommunityPremiumIsActive(CommunitySubscription? subscription, DateTime utcNow) =>
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    /// <summary>Benefícios ao nível do espaço (independentemente de quem consulta).</summary>
    public static bool CanAccessCommunityPremiumFeatures(CommunitySubscription? subscription, DateTime utcNow) =>
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    /// <summary>Analytics e métricas de comunidade: requer comunidade premium e papel de moderação.</summary>
    public static bool CanAccessCommunityAnalytics(CommunityMembership? membership, CommunitySubscription? subscription,
        DateTime utcNow) =>
        IsCommunityStaff(membership?.Role) &&
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    /// <summary>Impulsionar post no contexto da comunidade: staff + espaço premium.</summary>
    public static bool CanBoostCommunityPost(CommunityMembership? membership, CommunitySubscription? subscription,
        DateTime utcNow) =>
        IsCommunityStaff(membership?.Role) &&
        CommunitySubscriptionEntitlement.HasActivePremiumBenefits(subscription, utcNow);

    private static bool IsCommunityStaff(string? role) =>
        string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
}
