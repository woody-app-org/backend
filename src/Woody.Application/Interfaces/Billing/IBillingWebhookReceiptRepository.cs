namespace Woody.Application.Interfaces.Billing;

/// <summary>Idempotência de webhooks Stripe (evita processar o mesmo <c>evt_</c> duas vezes).</summary>
public interface IBillingWebhookReceiptRepository
{
    /// <summary>Tenta inserir o evento; devolve <c>false</c> se já existia (entrega duplicada).</summary>
    Task<bool> TryClaimEventAsync(string stripeEventId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>Remove o registo para permitir novo processamento após falha transitória.</summary>
    Task ReleaseClaimAsync(string stripeEventId, CancellationToken cancellationToken = default);
}
