namespace Woody.Domain.Entities.Enum;

/// <summary>Tipo semântico da notificação (persistido como inteiro na base).</summary>
public enum NotificationType
{
    PostLike = 1,
    PostComment = 2,
    CommentReply = 3,
    NewFollower = 4,
    ProfileSignal = 5,
    MessageRequest = 6,
    CommunityRequest = 7,
    CommunityRequestApproved = 8,
    PostShared = 9
}
