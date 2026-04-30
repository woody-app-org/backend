using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class UserNotificationRepository : IUserNotificationRepository
{
    private readonly WoodyDbContext _db;

    public UserNotificationRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public void Add(UserNotification row) => _db.UserNotifications.Add(row);

    public void AddRange(IEnumerable<UserNotification> rows) => _db.UserNotifications.AddRange(rows);

    public async Task<(List<UserNotification> Items, int Total)> ListForRecipientPagedAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = _db.UserNotifications.AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId)
            .Include(n => n.ActorUser)
            .ThenInclude(u => u!.Subscription)
            .OrderByDescending(n => n.CreatedAtUtc);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> CountUnreadForRecipientAsync(int recipientUserId, CancellationToken cancellationToken = default) =>
        _db.UserNotifications.CountAsync(
            n => n.RecipientUserId == recipientUserId && n.ReadAtUtc == null,
            cancellationToken);

    public Task<UserNotification?> GetTrackedForRecipientAsync(int id, int recipientUserId, CancellationToken cancellationToken = default) =>
        _db.UserNotifications.FirstOrDefaultAsync(
            n => n.Id == id && n.RecipientUserId == recipientUserId,
            cancellationToken);

    public async Task MarkAllReadForRecipientAsync(int recipientUserId, DateTime readAtUtc, CancellationToken cancellationToken = default)
    {
        await _db.UserNotifications
            .Where(n => n.RecipientUserId == recipientUserId && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, readAtUtc), cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
