namespace Woody.Application.Interfaces.Billing;

public enum StripeWebhookProcessOutcome
{
    /// <summary>Evento processado e alterações persistidas.</summary>
    Processed,

    /// <summary>Evento já tinha sido processado (idempotência).</summary>
    DuplicateDelivery,

    /// <summary>Tipo de evento ignorado (não mapeado; resposta 2xx para o Stripe).</summary>
    IgnoredEventType,

    /// <summary>Assinatura Stripe ou payload inválido.</summary>
    InvalidPayload,

    /// <summary>Assinatura HTTP inválida ou corpo adulterado.</summary>
    InvalidSignature,

    /// <summary>Webhook secret ou config em falta.</summary>
    NotConfigured,

    /// <summary>Falha inesperada durante o processamento (Stripe pode repetir).</summary>
    TransientFailure
}

/// <summary>Processamento centralizado de webhooks de billing Stripe.</summary>
public interface IStripeWebhookBillingProcessor
{
    Task<StripeWebhookProcessOutcome> ProcessAsync(string requestBody, string stripeSignatureHeader,
        CancellationToken cancellationToken = default);
}
