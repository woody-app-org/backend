namespace Woody.Application.Billing;

/// <summary>Resultado da leitura de uma subscrição Stripe: ramo utilizadora ou ramo comunidade (mutuamente exclusivo).</summary>
public sealed record BillingSubscriptionReadResult(
    BillingSubscriptionSnapshot? User,
    CommunityBillingSubscriptionSnapshot? Community,
    /// <summary>Preenchido quando o Stripe devolve <c>woody_community_id</c> na subscrição (checkout comunidade).</summary>
    int? WoodyCommunityIdFromStripeMetadata = null);
