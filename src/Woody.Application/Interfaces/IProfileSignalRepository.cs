using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface IProfileSignalRepository
{
    Task<ProfileSignal?> GetByIdWithUsersAsync(int id, CancellationToken cancellationToken = default);
    Task<ProfileSignal?> GetLatestOfTypeBetweenAsync(
        int senderUserId,
        int receiverUserId,
        ProfileSignalType type,
        CancellationToken cancellationToken = default);

    Task<bool> HasSentTypeSinceAsync(
        int senderUserId,
        int receiverUserId,
        ProfileSignalType type,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task<(List<ProfileSignal> Items, int Total)> ListReceivedInboxPagedAsync(
        int receiverUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(List<ProfileSignal> Items, int Total)> ListSentPagedAsync(
        int senderUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadReceivedAsync(int receiverUserId, CancellationToken cancellationToken = default);

    void Add(ProfileSignal signal);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
