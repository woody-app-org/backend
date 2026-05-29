using System.Text.Json;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Notifications;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public sealed class NotificationService : INotificationService
{
    /// <summary>Janela em que um segundo like no mesmo post pelo mesmo utilizador não gera nova notificação.</summary>
    private static readonly TimeSpan PostLikeDedupeWindow = TimeSpan.FromMinutes(15);

    private readonly INotificationRepository _notifications;
    private readonly INotificationRealtimePublisher _realtime;
    private readonly IUserRepository _users;
    private readonly IPostRepository _posts;
    private readonly IUserRelationshipVisibilityService _visibility;

    public NotificationService(
        INotificationRepository notifications,
        INotificationRealtimePublisher realtime,
        IUserRepository users,
        IPostRepository posts,
        IUserRelationshipVisibilityService visibility)
    {
        _notifications = notifications;
        _realtime = realtime;
        _users = users;
        _posts = posts;
        _visibility = visibility;
    }

    public async Task<NotificationListResponseDto> ListMineAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var hiddenActorIds = await _visibility.GetHiddenUserIdsForViewerAsync(recipientUserId, cancellationToken);
        var excludeActors = hiddenActorIds.Count > 0 ? hiddenActorIds : null;

        var (items, total) = await _notifications.ListForRecipientPagedAsync(
            recipientUserId,
            page,
            pageSize,
            excludeActors,
            cancellationToken);
        var dtos = items.Select(n =>
        {
            var metaEl = ParseMetadata(n.MetadataJson);
            return new NotificationItemDto
            {
                Id = n.Id.ToString(),
                Type = NotificationTypeApiMap.ToApiString(n.Type),
                TargetType = NotificationTargetKindApiMap.ToApiString(n.TargetKind),
                TargetId = n.TargetId,
                Title = n.Title,
                Message = n.Message,
                Metadata = metaEl,
                Payload = metaEl,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                Actor = NotificationActorDto.FromUserPublic(
                    n.ActorUser != null ? EntityMappers.ToUserPublicDto(n.ActorUser) : null)
            };
        }).ToList();

        return new NotificationListResponseDto
        {
            Items = dtos,
            Total = total,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 50)
        };
    }

    public async Task<int> GetUnreadCountAsync(int recipientUserId, CancellationToken cancellationToken = default)
    {
        var hiddenActorIds = await _visibility.GetHiddenUserIdsForViewerAsync(recipientUserId, cancellationToken);
        var excludeActors = hiddenActorIds.Count > 0 ? hiddenActorIds : null;
        return await _notifications.CountUnreadForRecipientAsync(recipientUserId, excludeActors, cancellationToken);
    }

    public async Task<bool> TryMarkReadAsync(int recipientUserId, int notificationId, CancellationToken cancellationToken = default)
    {
        var row = await _notifications.GetTrackedForRecipientAsync(notificationId, recipientUserId, cancellationToken);
        if (row == null)
            return false;
        if (row.ReadAt == null)
        {
            row.ReadAt = DateTime.UtcNow;
            await _notifications.SaveChangesAsync(cancellationToken);
            await _realtime.PublishInboxChangedAsync(recipientUserId, cancellationToken);
        }

        return true;
    }

    public async Task MarkAllReadAsync(int recipientUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _notifications.MarkAllReadForRecipientAsync(recipientUserId, now, cancellationToken);
        await _realtime.PublishInboxChangedAsync(recipientUserId, cancellationToken);
    }

    public async Task NotifyPostLikedAsync(int actorUserId, int postOwnerUserId, int postId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == postOwnerUserId)
            return;

        if (await ShouldSuppressBetweenUsersAsync(actorUserId, postOwnerUserId, cancellationToken))
            return;

        var now = DateTime.UtcNow;
        if (await _notifications.HasRecentPostLikeToRecipientAsync(
                postOwnerUserId,
                actorUserId,
                postId,
                now - PostLikeDedupeWindow,
                cancellationToken))
            return;

        _notifications.Add(NotificationComposer.PostLiked(
            postOwnerUserId,
            actorUserId,
            postId,
            now,
            await ResolvePostPublicIdAsync(postId, cancellationToken)));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(postOwnerUserId, cancellationToken);
    }

    public async Task NotifyPostCommentAsync(int actorUserId, int postOwnerUserId, int postId, int commentId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == postOwnerUserId)
            return;

        if (await ShouldSuppressBetweenUsersAsync(actorUserId, postOwnerUserId, cancellationToken))
            return;

        _notifications.Add(NotificationComposer.PostCommented(
            postOwnerUserId,
            actorUserId,
            postId,
            commentId,
            DateTime.UtcNow,
            await ResolvePostPublicIdAsync(postId, cancellationToken)));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(postOwnerUserId, cancellationToken);
    }

    public async Task NotifyCommentReplyAsync(
        int actorUserId,
        int parentCommentAuthorUserId,
        int postId,
        int parentCommentId,
        int replyCommentId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == parentCommentAuthorUserId)
            return;

        if (await ShouldSuppressBetweenUsersAsync(actorUserId, parentCommentAuthorUserId, cancellationToken))
            return;

        _notifications.Add(NotificationComposer.CommentReplied(
            parentCommentAuthorUserId,
            actorUserId,
            postId,
            parentCommentId,
            replyCommentId,
            DateTime.UtcNow,
            await ResolvePostPublicIdAsync(postId, cancellationToken)));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(parentCommentAuthorUserId, cancellationToken);
    }

    public async Task NotifyNewFollowerAsync(int actorUserId, int followedUserId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == followedUserId)
            return;

        if (await ShouldSuppressBetweenUsersAsync(actorUserId, followedUserId, cancellationToken))
            return;

        _notifications.Add(NotificationComposer.NewFollower(
            followedUserId,
            actorUserId,
            DateTime.UtcNow,
            await ResolveUsernameAsync(actorUserId, cancellationToken)));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(followedUserId, cancellationToken);
    }

    public async Task NotifyProfileSignalAsync(
        int senderUserId,
        int receiverUserId,
        int profileSignalId,
        string profileSignalTypeApi,
        CancellationToken cancellationToken = default)
    {
        if (senderUserId == receiverUserId)
            return;

        if (await ShouldSuppressBetweenUsersAsync(senderUserId, receiverUserId, cancellationToken))
            return;

        _notifications.Add(NotificationComposer.ProfileSignalReceived(
            receiverUserId,
            senderUserId,
            profileSignalId,
            profileSignalTypeApi,
            DateTime.UtcNow,
            await ResolveUsernameAsync(senderUserId, cancellationToken)));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(receiverUserId, cancellationToken);
    }

    public async Task NotifyMessageRequestAsync(int initiatorUserId, int recipientUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        if (initiatorUserId == recipientUserId)
            return;

        if (await ShouldSuppressBetweenUsersAsync(initiatorUserId, recipientUserId, cancellationToken))
            return;

        _notifications.Add(NotificationComposer.MessageRequest(
            recipientUserId,
            initiatorUserId,
            conversationId,
            DateTime.UtcNow,
            await ResolveUsernameAsync(initiatorUserId, cancellationToken)));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(recipientUserId, cancellationToken);
    }

    public async Task NotifyCommunityJoinRequestAsync(
        int requesterUserId,
        int communityId,
        string communitySlug,
        int joinRequestId,
        IReadOnlyList<int> moderatorUserIds,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var requesterUsername = await ResolveUsernameAsync(requesterUserId, cancellationToken);
        var rows = new List<Notification>();
        foreach (var modId in moderatorUserIds.Distinct())
        {
            if (modId == requesterUserId)
                continue;

            rows.Add(NotificationComposer.CommunityJoinRequest(
                modId,
                requesterUserId,
                communityId,
                communitySlug,
                joinRequestId,
                now,
                requesterUsername));
        }

        if (rows.Count == 0)
            return;

        _notifications.AddRange(rows);
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedManyAsync(rows.Select(r => r.RecipientUserId), cancellationToken);
    }

    public async Task NotifyCommunityRequestApprovedAsync(
        int applicantUserId,
        int? approverUserId,
        int communityId,
        string communitySlug,
        int joinRequestId,
        CancellationToken cancellationToken = default)
    {
        _notifications.Add(NotificationComposer.CommunityRequestApproved(
            applicantUserId,
            approverUserId,
            communityId,
            communitySlug,
            joinRequestId,
            DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
        await _realtime.PublishInboxChangedAsync(applicantUserId, cancellationToken);
    }

    private async Task<bool> ShouldSuppressBetweenUsersAsync(
        int actorUserId,
        int recipientUserId,
        CancellationToken cancellationToken) =>
        await _visibility.AreUsersBlockedEitherWayAsync(actorUserId, recipientUserId, cancellationToken);

    private async Task<string?> ResolvePostPublicIdAsync(int postId, CancellationToken cancellationToken)
    {
        var post = await _posts.GetByIdNonDeletedForCommentLookupAsync(postId, cancellationToken);
        return post?.PublicId;
    }

    private async Task<string?> ResolveUsernameAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdNoTrackingAsync(userId, cancellationToken);
        return user?.Username;
    }

    private static JsonElement ParseMetadata(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return doc.RootElement.Clone();
        }
        catch
        {
            using var doc = JsonDocument.Parse("{}");
            return doc.RootElement.Clone();
        }
    }
}
