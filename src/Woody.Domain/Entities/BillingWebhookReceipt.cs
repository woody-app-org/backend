namespace Woody.Domain.Entities;

/// <summary>Registo idempotente de entrega de webhook Stripe (<c>evt_…</c>).</summary>
public class BillingWebhookReceipt
{
    public string EventId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public DateTime ReceivedAtUtc { get; set; }
}
