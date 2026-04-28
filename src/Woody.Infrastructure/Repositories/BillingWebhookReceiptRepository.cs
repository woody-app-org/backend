using Microsoft.EntityFrameworkCore;
using Npgsql;
using Woody.Application.Interfaces.Billing;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class BillingWebhookReceiptRepository : IBillingWebhookReceiptRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly WoodyDbContext _context;

    public BillingWebhookReceiptRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryClaimEventAsync(string stripeEventId, string eventType,
        CancellationToken cancellationToken = default)
    {
        _context.BillingWebhookReceipts.Add(new BillingWebhookReceipt
        {
            EventId = stripeEventId,
            EventType = eventType,
            ReceivedAtUtc = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            foreach (var entry in _context.ChangeTracker.Entries<BillingWebhookReceipt>()
                         .Where(e => e.Entity.EventId == stripeEventId).ToList())
                entry.State = EntityState.Detached;

            return false;
        }
    }

    public Task ReleaseClaimAsync(string stripeEventId, CancellationToken cancellationToken = default) =>
        _context.BillingWebhookReceipts.Where(e => e.EventId == stripeEventId).ExecuteDeleteAsync(cancellationToken);

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
}
