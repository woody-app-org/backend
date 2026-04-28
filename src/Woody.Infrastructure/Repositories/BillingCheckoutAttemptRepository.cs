using Microsoft.EntityFrameworkCore;
using Npgsql;
using Woody.Application.Interfaces.Billing;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class BillingCheckoutAttemptRepository : IBillingCheckoutAttemptRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly WoodyDbContext _context;

    public BillingCheckoutAttemptRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task<BillingCheckoutAttempt?> GetReusableAsync(
        string idempotencyKey,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        _context.BillingCheckoutAttempts
            .FirstOrDefaultAsync(x =>
                x.IdempotencyKey == idempotencyKey
                && x.Status == BillingCheckoutAttemptStatus.Pending
                && x.ExpiresAtUtc > utcNow
                && x.StripeSessionUrl != null,
                cancellationToken);

    public async Task<BillingCheckoutAttempt> ClaimOrGetAsync(
        string idempotencyKey,
        int userId,
        BillingCheckoutAttemptSubjectKind subjectKind,
        string planCode,
        int? communityId,
        DateTime utcNow,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var attempt = new BillingCheckoutAttempt
        {
            IdempotencyKey = idempotencyKey,
            UserId = userId,
            SubjectKind = subjectKind,
            PlanCode = planCode,
            CommunityId = communityId,
            Status = BillingCheckoutAttemptStatus.Pending,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        _context.BillingCheckoutAttempts.Add(attempt);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return attempt;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            Detach(attempt);
            var existing = await _context.BillingCheckoutAttempts
                .FirstAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existing.ExpiresAtUtc <= utcNow || existing.Status == BillingCheckoutAttemptStatus.Failed)
            {
                existing.UserId = userId;
                existing.SubjectKind = subjectKind;
                existing.PlanCode = planCode;
                existing.CommunityId = communityId;
                existing.StripeSessionId = null;
                existing.StripeSessionUrl = null;
                existing.StripeCustomerId = null;
                existing.Status = BillingCheckoutAttemptStatus.Pending;
                existing.ExpiresAtUtc = expiresAtUtc;
                existing.UpdatedAtUtc = utcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }
    }

    public void MarkSessionCreated(
        BillingCheckoutAttempt attempt,
        string stripeSessionId,
        string stripeSessionUrl,
        string? stripeCustomerId,
        DateTime utcNow)
    {
        attempt.StripeSessionId = stripeSessionId;
        attempt.StripeSessionUrl = stripeSessionUrl;
        attempt.StripeCustomerId = stripeCustomerId;
        attempt.Status = BillingCheckoutAttemptStatus.Pending;
        attempt.UpdatedAtUtc = utcNow;
        _context.BillingCheckoutAttempts.Update(attempt);
    }

    public void MarkFailed(BillingCheckoutAttempt attempt, DateTime utcNow)
    {
        attempt.Status = BillingCheckoutAttemptStatus.Failed;
        attempt.UpdatedAtUtc = utcNow;
        _context.BillingCheckoutAttempts.Update(attempt);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        IsPostgresUniqueViolation(ex) || IsSqliteUniqueViolation(ex);

    private static bool IsPostgresUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == UniqueViolationSqlState;

    private static bool IsSqliteUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException?.GetType().FullName != "Microsoft.Data.Sqlite.SqliteException")
            return false;

        var errorCode = ex.InnerException.GetType().GetProperty("SqliteErrorCode")?.GetValue(ex.InnerException);
        return errorCode is 19;
    }

    private void Detach(BillingCheckoutAttempt attempt)
    {
        var entry = _context.Entry(attempt);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
