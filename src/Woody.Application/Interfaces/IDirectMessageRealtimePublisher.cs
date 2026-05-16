using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

/// <summary>Abstrai SignalR para o domínio de mensagens diretas (implementação na API).</summary>
public interface IDirectMessageRealtimePublisher
{
    Task BroadcastMessageCreatedAsync(
        MessageResponseDto message,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default);

    Task BroadcastMessageUpdatedAsync(
        MessageResponseDto message,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default);

    Task BroadcastMessageDeletedAsync(
        MessageResponseDto message,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default);

    Task BroadcastConversationUpdatedAsync(
        ConversationRealtimeDto conversation,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default);
}
