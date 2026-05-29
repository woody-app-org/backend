using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUserBlockRepository
{
    Task<bool> ExistsAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default);

    Task<bool> AreBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetHiddenUserIdsForViewerAsync(int viewerId, CancellationToken cancellationToken = default);

    Task<(List<User> Items, int Total)> ListBlockedUsersPagedAsync(
        int blockerUserId,
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    void Add(UserBlock block);

    Task<UserBlock?> GetAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default);

    void Remove(UserBlock block);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
