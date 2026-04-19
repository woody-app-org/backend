using Woody.Application.Billing;

namespace Woody.Application.Interfaces.Billing;

/// <summary>Validação de webhooks do gateway (base para fila idempotente em etapas futuras).</summary>
public interface IBillingWebhookSignatureVerifier
{
    BillingWebhookValidationResult Validate(string requestBody, string stripeSignatureHeader);
}
