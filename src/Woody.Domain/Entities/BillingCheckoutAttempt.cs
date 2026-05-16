using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

public class BillingCheckoutAttempt
{
    public int Id { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public int UserId { get; set; }
    public BillingCheckoutAttemptSubjectKind SubjectKind { get; set; }
    public string PlanCode { get; set; } = null!;
    public int? CommunityId { get; set; }
    public string? StripeSessionId { get; set; }
    public string? StripeSessionUrl { get; set; }
    public string? StripeCustomerId { get; set; }
    public BillingCheckoutAttemptStatus Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
