using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface INotificationRepository
{
    /// <summary>
    /// Evita spam de <see cref="NotificationType.PostLike"/> (ex.: like → unlike → like no mesmo post).
    /// </summary>
    Task<bool> HasRecentPostLikeToRecipientAsync(
        int recipientUserId,
        int actorUserId,
        int postId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    void Add(Notification row);

    void AddRange(IEnumerable<Notification> rows);

    Task<(List<Notification> Items, int Total)> ListForRecipientPagedAsync(
        int recipientUserId,
        int page,
        int pageSize,
        IReadOnlyCollection<int>? excludeActorUserIds = null,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadForRecipientAsync(
        int recipientUserId,
        IReadOnlyCollection<int>? excludeActorUserIds = null,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetTrackedForRecipientAsync(int id, int recipientUserId, CancellationToken cancellationToken = default);

    Task MarkAllReadForRecipientAsync(int recipientUserId, DateTime readAtUtc, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
