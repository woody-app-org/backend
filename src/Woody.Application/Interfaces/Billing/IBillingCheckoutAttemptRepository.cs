using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces.Billing;

public interface IBillingCheckoutAttemptRepository
{
    Task<BillingCheckoutAttempt?> GetReusableAsync(
        string idempotencyKey,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<BillingCheckoutAttempt> ClaimOrGetAsync(
        string idempotencyKey,
        int userId,
        BillingCheckoutAttemptSubjectKind subjectKind,
        string planCode,
        int? communityId,
        DateTime utcNow,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    void MarkSessionCreated(
        BillingCheckoutAttempt attempt,
        string stripeSessionId,
        string stripeSessionUrl,
        string? stripeCustomerId,
        DateTime utcNow);

    void MarkFailed(BillingCheckoutAttempt attempt, DateTime utcNow);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
