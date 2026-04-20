using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public interface IDirectMessagingService
{
    Task<ConversationResponseDto> StartOrGetConversationAsync(
        int actorUserId,
        int otherUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationResponseDto>> ListMyConversationsAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationResponseDto>> ListPendingRequestsReceivedAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<ConversationResponseDto> GetConversationForParticipantAsync(
        int actorUserId,
        int conversationId,
        CancellationToken cancellationToken = default);

    Task<ConversationResponseDto> AcceptPendingAsync(int actorUserId, int conversationId, CancellationToken cancellationToken = default);

    Task<ConversationResponseDto> RejectPendingAsync(int actorUserId, int conversationId, CancellationToken cancellationToken = default);
}
