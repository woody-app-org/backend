namespace Woody.Application.Billing;

/// <summary>Chaves de metadata Stripe (checkout + subscription) para encaminhar webhooks sem duplicar strings.</summary>
public static class StripeBillingMetadataKeys
{
    public const string WoodyUserId = "woody_user_id";
    public const string PlanCode = "plan_code";
    public const string WoodyBillingSubject = "woody_billing_subject";
    public const string WoodyCommunityId = "woody_community_id";

    /// <summary>Assinatura Woody Pro da utilizadora (preço utilizador).</summary>
    public const string SubjectUserPro = "user_pro";

    /// <summary>Assinatura premium do espaço (preço comunidade; pagadora é a utilizadora em <see cref="WoodyUserId"/>).</summary>
    public const string SubjectCommunityPremium = "community_premium";
}
