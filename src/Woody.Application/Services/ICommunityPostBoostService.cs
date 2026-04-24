using Woody.Application.DTOs.Api;

namespace Woody.Application.Services;

public interface ICommunityPostBoostService
{
    Task<(CommunityPostBoostResponseDto? dto, string? error)> ActivateAsync(
        int communityId,
        int postId,
        int actorUserId,
        int? durationDays,
        CancellationToken cancellationToken = default);

    Task<(bool ok, string? error)> DeactivateAsync(
        int communityId,
        int postId,
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CommunityPostBoostListItemDto> items, string? error)> ListActiveAsync(
        int communityId,
        int actorUserId,
        CancellationToken cancellationToken = default);
}
