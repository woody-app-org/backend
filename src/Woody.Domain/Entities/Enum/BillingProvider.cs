namespace Woody.Domain.Entities.Enum;

/// <summary>
/// Gateway de cobrança associado aos identificadores externos em <see cref="UserSubscription"/> e <see cref="CommunitySubscription"/>.
/// </summary>
public enum BillingProvider
{
    None = 0,
    Stripe = 1
}
