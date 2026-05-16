using Woody.Domain.Entities.Enum;

namespace Woody.Application.Notifications;

/// <summary>Mapeamento estável enum → string da API (snake_case), alinhado ao contrato do frontend.</summary>
public static class NotificationTypeApiMap
{
    public static string ToApiString(NotificationType type) => type switch
    {
        NotificationType.PostLike => "post_like",
        NotificationType.PostComment => "post_comment",
        NotificationType.CommentReply => "comment_reply",
        NotificationType.NewFollower => "new_follower",
        NotificationType.ProfileSignal => "profile_signal",
        NotificationType.MessageRequest => "message_request",
        NotificationType.CommunityRequest => "community_request",
        NotificationType.CommunityRequestApproved => "community_request_approved",
        _ => type.ToString().ToLowerInvariant()
    };
}

public static class NotificationTargetKindApiMap
{
    public static string ToApiString(NotificationTargetKind kind) => kind switch
    {
        NotificationTargetKind.None => "none",
        NotificationTargetKind.Post => "post",
        NotificationTargetKind.Comment => "comment",
        NotificationTargetKind.User => "user",
        NotificationTargetKind.Conversation => "conversation",
        NotificationTargetKind.Community => "community",
        NotificationTargetKind.JoinRequest => "join_request",
        NotificationTargetKind.ProfileSignal => "profile_signal",
        _ => kind.ToString().ToLowerInvariant()
    };
}
