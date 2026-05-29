using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public interface IUserRelationshipVisibilityService
{
    Task<bool> AreUsersBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetHiddenUserIdsForViewerAsync(int viewerId, CancellationToken cancellationToken = default);

    Task BlockAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default);

    Task UnblockAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default);

    Task<PaginatedResponseDto<UserPublicDto>> ListBlockedByUserPagedAsync(
        int blockerUserId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
}
