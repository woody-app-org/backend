using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface INotificationRepository
{
    void Add(Notification row);

    void AddRange(IEnumerable<Notification> rows);

    Task<(List<Notification> Items, int Total)> ListForRecipientPagedAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadForRecipientAsync(int recipientUserId, CancellationToken cancellationToken = default);

    Task<Notification?> GetTrackedForRecipientAsync(int id, int recipientUserId, CancellationToken cancellationToken = default);

    Task MarkAllReadForRecipientAsync(int recipientUserId, DateTime readAtUtc, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
