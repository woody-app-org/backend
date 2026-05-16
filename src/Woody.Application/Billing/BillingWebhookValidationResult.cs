namespace Woody.Application.Billing;

/// <summary>Resultado da validação de assinatura de webhook (corpo bruto + header Stripe-Signature).</summary>
public sealed record BillingWebhookValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string? EventId,
    string? EventType,
    string? RawJson);
