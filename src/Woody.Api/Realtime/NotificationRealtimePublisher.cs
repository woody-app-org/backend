using Microsoft.AspNetCore.SignalR;
using Woody.Api.Hubs;
using Woody.Application.Interfaces;

namespace Woody.Api.Realtime;

public sealed class NotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<DirectMessagesHub> _hub;
    private readonly ILogger<NotificationRealtimePublisher> _logger;

    public NotificationRealtimePublisher(
        IHubContext<DirectMessagesHub> hub,
        ILogger<NotificationRealtimePublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task PublishInboxChangedAsync(int recipientUserId, CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            async ct =>
            {
                await _hub.Clients
                    .Group(DirectMessageHubGroups.UserInbox(recipientUserId))
                    .SendAsync("notificationsChanged", new { }, ct);
            },
            cancellationToken,
            recipientUserId);

    public async Task PublishInboxChangedManyAsync(
        IEnumerable<int> recipientUserIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var id in recipientUserIds.Distinct())
        {
            if (id < 1)
                continue;
            await PublishInboxChangedAsync(id, cancellationToken);
        }
    }

    private async Task SafeSendAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        int recipientUserId)
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
            _logger.LogWarning(ex, "Falha ao emitir notificationsChanged (destinatária {RecipientUserId}).", recipientUserId);
        }
    }
}
