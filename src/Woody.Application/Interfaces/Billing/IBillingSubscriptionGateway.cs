using Woody.Application.Billing;

namespace Woody.Application.Interfaces.Billing;

/// <summary>
/// Leitura de assinaturas no provedor (Stripe). A persistência autoritativa continua em
/// <see cref="Woody.Domain.Entities.UserSubscription"/> e <see cref="Woody.Domain.Entities.CommunitySubscription"/>.
/// </summary>
public interface IBillingSubscriptionGateway
{
    /// <summary>Obtém estado atual da subscrição no gateway (ramo utilizadora ou comunidade) ou null se indisponível.</summary>
    Task<BillingSubscriptionReadResult?> GetSubscriptionAsync(string providerSubscriptionId,
        CancellationToken cancellationToken = default);
}
