using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly WoodyDbContext _db;

    public NotificationRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public void Add(Notification row) => _db.Notifications.Add(row);

    public void AddRange(IEnumerable<Notification> rows) => _db.Notifications.AddRange(rows);

    public async Task<(List<Notification> Items, int Total)> ListForRecipientPagedAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId)
            .Include(n => n.ActorUser)
            .ThenInclude(u => u!.Subscription)
            .OrderByDescending(n => n.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> CountUnreadForRecipientAsync(int recipientUserId, CancellationToken cancellationToken = default) =>
        _db.Notifications.CountAsync(
            n => n.RecipientUserId == recipientUserId && n.ReadAt == null,
            cancellationToken);

    public Task<Notification?> GetTrackedForRecipientAsync(int id, int recipientUserId, CancellationToken cancellationToken = default) =>
        _db.Notifications.FirstOrDefaultAsync(
            n => n.Id == id && n.RecipientUserId == recipientUserId,
            cancellationToken);

    public async Task MarkAllReadForRecipientAsync(int recipientUserId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        await _db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, readAtUtc), cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
