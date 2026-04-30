using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUserNotificationRepository
{
    void Add(UserNotification row);

    void AddRange(IEnumerable<UserNotification> rows);

    Task<(List<UserNotification> Items, int Total)> ListForRecipientPagedAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadForRecipientAsync(int recipientUserId, CancellationToken cancellationToken = default);

    Task<UserNotification?> GetTrackedForRecipientAsync(int id, int recipientUserId, CancellationToken cancellationToken = default);

    Task MarkAllReadForRecipientAsync(int recipientUserId, DateTime readAtUtc, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
