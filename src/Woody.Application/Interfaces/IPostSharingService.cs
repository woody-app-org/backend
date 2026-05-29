using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public interface IPostSharingService
{
    Task<SharePostToConversationResponseDto> ShareToConversationAsync(
        int actorUserId,
        int postId,
        SharePostToConversationRequestDto request,
        CancellationToken cancellationToken = default);
}
