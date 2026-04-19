namespace Woody.Domain.Entities.Enum;

/// <summary>
/// Gateway de cobrança associado aos identificadores externos em <see cref="UserSubscription"/>.
/// </summary>
public enum BillingProvider
{
    None = 0,
    Stripe = 1
}
