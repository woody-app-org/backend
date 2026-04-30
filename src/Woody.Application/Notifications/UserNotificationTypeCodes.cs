namespace Woody.Application.Notifications;

/// <summary>Valores estáveis do campo <see cref="Domain.Entities.UserNotification.Type"/> (API + persistência).</summary>
public static class UserNotificationTypeCodes
{
    public const string PostLike = "post_like";
    public const string PostComment = "post_comment";
    public const string CommentReply = "comment_reply";
    public const string NewFollower = "new_follower";
    public const string ProfileSignal = "profile_signal";
    public const string MessageRequest = "message_request";
    public const string CommunityRequest = "community_request";
    public const string CommunityRequestApproved = "community_request_approved";
}
