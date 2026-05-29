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

    Task<ConversationMessagesPageDto> ListMessagesAsync(
        int actorUserId,
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MessageResponseDto> SendMessageAsync(
        int actorUserId,
        int conversationId,
        SendConversationMessageRequestDto body,
        CancellationToken cancellationToken = default);

    Task<MessageResponseDto> SendSharedPostMessageAsync(
        int actorUserId,
        int conversationId,
        int sharedPostId,
        string? body,
        CancellationToken cancellationToken = default);

    Task<MessageResponseDto> EditMessageAsync(
        int actorUserId,
        int conversationId,
        int messageId,
        EditConversationMessageRequestDto body,
        CancellationToken cancellationToken = default);

    Task DeleteMessageAsync(
        int actorUserId,
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default);
}
