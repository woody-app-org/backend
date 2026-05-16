namespace Woody.Application.Interfaces;

/// <summary>
/// Emite aviso leve para a destinatária (ex.: SignalR inbox) para atualizar contador/lista sem expor o payload da notificação.
/// </summary>
public interface INotificationRealtimePublisher
{
    Task PublishInboxChangedAsync(int recipientUserId, CancellationToken cancellationToken = default);

    Task PublishInboxChangedManyAsync(IEnumerable<int> recipientUserIds, CancellationToken cancellationToken = default);
}
