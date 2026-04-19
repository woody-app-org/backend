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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                             pg.SqlState == UniqueViolationSqlState)
        {
            foreach (var entry in _context.ChangeTracker.Entries<BillingWebhookReceipt>()
                         .Where(e => e.Entity.EventId == stripeEventId).ToList())
                entry.State = EntityState.Detached;

            return false;
        }
    }

    public Task ReleaseClaimAsync(string stripeEventId, CancellationToken cancellationToken = default) =>
        _context.BillingWebhookReceipts.Where(e => e.EventId == stripeEventId).ExecuteDeleteAsync(cancellationToken);
}
