using System.Text.Json;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Notifications;

/// <summary>
/// Constrói instâncias de <see cref="Notification"/> e metadados JSON — único sítio para regras de preenchimento.
/// </summary>
public static class NotificationComposer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static Notification PostLiked(
        int recipientUserId,
        int actorUserId,
        int postId,
        DateTime createdAtUtc,
        string? postPublicId = null) =>
        new()
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = NotificationType.PostLike,
            TargetKind = NotificationTargetKind.Post,
            TargetId = postId,
            MetadataJson = Meta(BuildPostMetadata(postId, postPublicId: postPublicId)),
            CreatedAt = createdAtUtc
        };

    public static Notification PostCommented(
        int recipientUserId,
        int actorUserId,
        int postId,
        int commentId,
        DateTime createdAtUtc,
        string? postPublicId = null) =>
        new()
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = NotificationType.PostComment,
            TargetKind = NotificationTargetKind.Post,
            TargetId = postId,
            MetadataJson = Meta(BuildPostMetadata(postId, commentId, postPublicId)),
            CreatedAt = createdAtUtc
        };

    public static Notification CommentReplied(
        int recipientUserId,
        int actorUserId,
        int postId,
        int parentCommentId,
        int replyCommentId,
        DateTime createdAtUtc,
        string? postPublicId = null) =>
        new()
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = NotificationType.CommentReply,
            TargetKind = NotificationTargetKind.Comment,
            TargetId = parentCommentId,
            MetadataJson = Meta(new Dictionary<string, object?>
            {
                ["postId"] = postId,
                ["postPublicId"] = string.IsNullOrWhiteSpace(postPublicId) ? null : postPublicId.Trim(),
                ["parentCommentId"] = parentCommentId,
                ["commentId"] = replyCommentId
            }),
            CreatedAt = createdAtUtc
        };

    public static Notification NewFollower(
        int recipientUserId,
        int actorUserId,
        DateTime createdAtUtc,
        string? actorUsername = null) =>
        new()
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = NotificationType.NewFollower,
            TargetKind = NotificationTargetKind.User,
            TargetId = actorUserId,
            MetadataJson = Meta(BuildActorProfileMetadata(actorUserId, actorUsername)),
            CreatedAt = createdAtUtc
        };

    public static Notification ProfileSignalReceived(
        int recipientUserId,
        int senderUserId,
        int profileSignalId,
        string profileSignalTypeApi,
        DateTime createdAtUtc,
        string? senderUsername = null) =>
        new()
        {
            RecipientUserId = recipientUserId,
            ActorUserId = senderUserId,
            Type = NotificationType.ProfileSignal,
            TargetKind = NotificationTargetKind.ProfileSignal,
            TargetId = profileSignalId,
            MetadataJson = Meta(Merge(
                BuildActorProfileMetadata(senderUserId, senderUsername),
                new Dictionary<string, object?>
                {
                    ["profileUserId"] = senderUserId,
                    ["profileSignalId"] = profileSignalId,
                    ["profileSignalType"] = profileSignalTypeApi
                })),
            CreatedAt = createdAtUtc
        };

    public static Notification MessageRequest(
        int recipientUserId,
        int initiatorUserId,
        int conversationId,
        DateTime createdAtUtc,
        string? initiatorUsername = null) =>
        new()
        {
            RecipientUserId = recipientUserId,
            ActorUserId = initiatorUserId,
            Type = NotificationType.MessageRequest,
            TargetKind = NotificationTargetKind.Conversation,
            TargetId = conversationId,
            MetadataJson = Meta(Merge(
                BuildActorProfileMetadata(initiatorUserId, initiatorUsername),
                new Dictionary<string, object?> { ["conversationId"] = conversationId })),
            CreatedAt = createdAtUtc
        };

    public static Notification CommunityJoinRequest(
        int moderatorUserId,
        int requesterUserId,
        int communityId,
        string communitySlug,
        int joinRequestId,
        DateTime createdAtUtc,
        string? requesterUsername = null) =>
        new()
        {
            RecipientUserId = moderatorUserId,
            ActorUserId = requesterUserId,
            Type = NotificationType.CommunityRequest,
            TargetKind = NotificationTargetKind.JoinRequest,
            TargetId = joinRequestId,
            MetadataJson = Meta(Merge(
                BuildActorProfileMetadata(requesterUserId, requesterUsername),
                new Dictionary<string, object?>
                {
                    ["communityId"] = communityId,
                    ["communitySlug"] = communitySlug,
                    ["joinRequestId"] = joinRequestId
                })),
            CreatedAt = createdAtUtc
        };

    public static Notification CommunityRequestApproved(
        int applicantUserId,
        int? approverUserId,
        int communityId,
        string communitySlug,
        int joinRequestId,
        DateTime createdAtUtc) =>
        new()
        {
            RecipientUserId = applicantUserId,
            ActorUserId = approverUserId is > 0 and var aid && aid != applicantUserId ? aid : null,
            Type = NotificationType.CommunityRequestApproved,
            TargetKind = NotificationTargetKind.Community,
            TargetId = communityId,
            MetadataJson = Meta(new Dictionary<string, object?>
            {
                ["communityId"] = communityId,
                ["communitySlug"] = communitySlug,
                ["joinRequestId"] = joinRequestId
            }),
            CreatedAt = createdAtUtc
        };

    private static Dictionary<string, object?> BuildPostMetadata(
        int postId,
        int? commentId = null,
        string? postPublicId = null)
    {
        var meta = new Dictionary<string, object?> { ["postId"] = postId };
        if (!string.IsNullOrWhiteSpace(postPublicId))
            meta["postPublicId"] = postPublicId.Trim();
        if (commentId.HasValue)
            meta["commentId"] = commentId.Value;
        return meta;
    }

    private static string Meta(Dictionary<string, object?> values) =>
        JsonSerializer.Serialize(values, JsonOptions);

    private static Dictionary<string, object?> BuildActorProfileMetadata(int actorUserId, string? actorUsername)
    {
        var meta = new Dictionary<string, object?>
        {
            ["profileUserId"] = actorUserId,
            ["actorUserId"] = actorUserId
        };
        if (!string.IsNullOrWhiteSpace(actorUsername))
            meta["actorUsername"] = actorUsername.Trim();
        return meta;
    }

    private static Dictionary<string, object?> Merge(
        Dictionary<string, object?> primary,
        Dictionary<string, object?> secondary)
    {
        foreach (var (key, value) in secondary)
            primary[key] = value;
        return primary;
    }
}
