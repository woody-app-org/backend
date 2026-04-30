namespace Woody.Domain.Entities.Enum;

/// <summary>Entidade principal referenciada por <see cref="Notification.TargetId"/> (opcional).</summary>
public enum NotificationTargetKind
{
    None = 0,
    Post = 1,
    Comment = 2,
    User = 3,
    Conversation = 4,
    Community = 5,
    JoinRequest = 6,
    ProfileSignal = 7
}
