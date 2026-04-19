using Woody.Application.Billing;

namespace Woody.Application.Interfaces.Billing;

/// <summary>
/// Leitura de assinaturas no provedor (Stripe). A persistência autoritativa continua em <see cref="Woody.Domain.Entities.UserSubscription"/>.
/// </summary>
public interface IBillingSubscriptionGateway
{
    /// <summary>Obtém estado atual da assinatura no gateway ou null se indisponível / não encontrada.</summary>
    Task<BillingSubscriptionSnapshot?> GetSubscriptionAsync(string providerSubscriptionId,
        CancellationToken cancellationToken = default);
}
