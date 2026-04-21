using Microsoft.AspNetCore.SignalR;
using Woody.Api.Hubs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;

namespace Woody.Api.Realtime;

public sealed class DirectMessageRealtimePublisher : IDirectMessageRealtimePublisher
{
    private readonly IHubContext<DirectMessagesHub> _hub;
    private readonly ILogger<DirectMessageRealtimePublisher> _logger;

    public DirectMessageRealtimePublisher(
        IHubContext<DirectMessagesHub> hub,
        ILogger<DirectMessageRealtimePublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task BroadcastMessageCreatedAsync(
        MessageResponseDto message,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default) =>
        SafeBroadcastAsync(
            async ct =>
            {
                await _hub.Clients
                    .Group(DirectMessageHubGroups.Conversation(message.ConversationId))
                    .SendAsync("messageCreated", message, ct);

                var inbox = new DirectMessageInboxEventDto { Kind = "message", ConversationId = message.ConversationId };
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userLowId)).SendAsync("inboxChanged", inbox, ct);
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userHighId)).SendAsync("inboxChanged", inbox, ct);
            },
            cancellationToken,
            "messageCreated");

    public Task BroadcastMessageUpdatedAsync(
        MessageResponseDto message,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default) =>
        SafeBroadcastAsync(
            async ct =>
            {
                await _hub.Clients
                    .Group(DirectMessageHubGroups.Conversation(message.ConversationId))
                    .SendAsync("messageUpdated", message, ct);

                var inbox = new DirectMessageInboxEventDto { Kind = "message", ConversationId = message.ConversationId };
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userLowId)).SendAsync("inboxChanged", inbox, ct);
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userHighId)).SendAsync("inboxChanged", inbox, ct);
            },
            cancellationToken,
            "messageUpdated");

    public Task BroadcastMessageDeletedAsync(
        MessageResponseDto message,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default) =>
        SafeBroadcastAsync(
            async ct =>
            {
                await _hub.Clients
                    .Group(DirectMessageHubGroups.Conversation(message.ConversationId))
                    .SendAsync("messageDeleted", message, ct);

                var inbox = new DirectMessageInboxEventDto { Kind = "message", ConversationId = message.ConversationId };
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userLowId)).SendAsync("inboxChanged", inbox, ct);
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userHighId)).SendAsync("inboxChanged", inbox, ct);
            },
            cancellationToken,
            "messageDeleted");

    public Task BroadcastConversationUpdatedAsync(
        ConversationRealtimeDto conversation,
        int userLowId,
        int userHighId,
        CancellationToken cancellationToken = default) =>
        SafeBroadcastAsync(
            async ct =>
            {
                await _hub.Clients
                    .Group(DirectMessageHubGroups.Conversation(conversation.Id))
                    .SendAsync("conversationUpdated", conversation, ct);

                var inbox = new DirectMessageInboxEventDto { Kind = "conversation", ConversationId = conversation.Id };
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userLowId)).SendAsync("inboxChanged", inbox, ct);
                await _hub.Clients.Group(DirectMessageHubGroups.UserInbox(userHighId)).SendAsync("inboxChanged", inbox, ct);
            },
            cancellationToken,
            "conversationUpdated");

    private async Task SafeBroadcastAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        string label)
    {
        try
        {
            await action(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // ignorar cancelamento de request HTTP
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir evento SignalR ({Label}).", label);
        }
    }
}
