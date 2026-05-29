using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public sealed class UserRelationshipVisibilityService : IUserRelationshipVisibilityService
{
    private readonly IUserBlockRepository _blocks;
    private readonly IFollowRepository _follows;
    private readonly IUserRepository _users;
    private readonly IStoryRepository _stories;

    public UserRelationshipVisibilityService(
        IUserBlockRepository blocks,
        IFollowRepository follows,
        IUserRepository users,
        IStoryRepository stories)
    {
        _blocks = blocks;
        _follows = follows;
        _users = users;
        _stories = stories;
    }

    public Task<bool> AreUsersBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default) =>
        _blocks.AreBlockedEitherWayAsync(userIdA, userIdB, cancellationToken);

    public Task<HashSet<int>> GetHiddenUserIdsForViewerAsync(int viewerId, CancellationToken cancellationToken = default) =>
        _blocks.GetHiddenUserIdsForViewerAsync(viewerId, cancellationToken);

    public async Task BlockAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        if (blockerUserId == blockedUserId)
            throw new ArgumentException("Não podes bloquear a ti própria.");

        if (await _users.GetByIdNoTrackingAsync(blockedUserId, cancellationToken) == null)
            throw new KeyNotFoundException("Utilizadora não encontrada.");

        if (!await _blocks.ExistsAsync(blockerUserId, blockedUserId, cancellationToken))
        {
            _blocks.Add(new UserBlock
            {
                BlockerUserId = blockerUserId,
                BlockedUserId = blockedUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await RemoveFollowBothWaysAsync(blockerUserId, blockedUserId, cancellationToken);
        await _blocks.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default)
    {
        var row = await _blocks.GetAsync(blockerUserId, blockedUserId, cancellationToken);
        if (row == null)
            return;

        _blocks.Remove(row);
        await _blocks.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedResponseDto<UserPublicDto>> ListBlockedByUserPagedAsync(
        int blockerUserId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _blocks.ListBlockedUsersPagedAsync(
            blockerUserId,
            page,
            pageSize,
            search,
            cancellationToken);

        var storyFlags = await _stories.GetUserIdsWithActiveStoriesAsync(
            items.Select(u => u.Id),
            cancellationToken);

        return new PaginatedResponseDto<UserPublicDto>
        {
            Items = items.Select(u => EntityMappers.ToUserPublicDto(u, storyFlags.Contains(u.Id))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        };
    }

    private async Task RemoveFollowBothWaysAsync(int userIdA, int userIdB, CancellationToken cancellationToken)
    {
        var aFollowsB = await _follows.GetAsync(userIdA, userIdB, cancellationToken);
        if (aFollowsB != null)
            _follows.Remove(aFollowsB);

        var bFollowsA = await _follows.GetAsync(userIdB, userIdA, cancellationToken);
        if (bFollowsA != null)
            _follows.Remove(bFollowsA);
    }
}
