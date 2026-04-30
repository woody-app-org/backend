using System.Text.Json;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Notifications;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<NotificationListResponseDto> ListMineAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _notifications.ListForRecipientPagedAsync(recipientUserId, page, pageSize, cancellationToken);
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
                CreatedAtUtc = n.CreatedAt,
                ReadAtUtc = n.ReadAt,
                Actor = n.ActorUser != null ? EntityMappers.ToUserPublicDto(n.ActorUser) : null
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

    public Task<int> GetUnreadCountAsync(int recipientUserId, CancellationToken cancellationToken = default) =>
        _notifications.CountUnreadForRecipientAsync(recipientUserId, cancellationToken);

    public async Task<bool> TryMarkReadAsync(int recipientUserId, int notificationId, CancellationToken cancellationToken = default)
    {
        var row = await _notifications.GetTrackedForRecipientAsync(notificationId, recipientUserId, cancellationToken);
        if (row == null)
            return false;
        if (row.ReadAt == null)
        {
            row.ReadAt = DateTime.UtcNow;
            await _notifications.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task MarkAllReadAsync(int recipientUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _notifications.MarkAllReadForRecipientAsync(recipientUserId, now, cancellationToken);
    }

    public async Task NotifyPostLikedAsync(int actorUserId, int postOwnerUserId, int postId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == postOwnerUserId)
            return;

        _notifications.Add(NotificationComposer.PostLiked(postOwnerUserId, actorUserId, postId, DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyPostCommentAsync(int actorUserId, int postOwnerUserId, int postId, int commentId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == postOwnerUserId)
            return;

        _notifications.Add(NotificationComposer.PostCommented(postOwnerUserId, actorUserId, postId, commentId, DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
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

        _notifications.Add(NotificationComposer.CommentReplied(
            parentCommentAuthorUserId,
            actorUserId,
            postId,
            parentCommentId,
            replyCommentId,
            DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyNewFollowerAsync(int actorUserId, int followedUserId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == followedUserId)
            return;

        _notifications.Add(NotificationComposer.NewFollower(followedUserId, actorUserId, DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
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

        _notifications.Add(NotificationComposer.ProfileSignalReceived(
            receiverUserId,
            senderUserId,
            profileSignalId,
            profileSignalTypeApi,
            DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyMessageRequestAsync(int initiatorUserId, int recipientUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        if (initiatorUserId == recipientUserId)
            return;

        _notifications.Add(NotificationComposer.MessageRequest(recipientUserId, initiatorUserId, conversationId, DateTime.UtcNow));
        await _notifications.SaveChangesAsync(cancellationToken);
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
        var rows = new List<Notification>();
        foreach (var modId in moderatorUserIds.Distinct())
        {
            if (modId == requesterUserId)
                continue;

            rows.Add(NotificationComposer.CommunityJoinRequest(modId, requesterUserId, communityId, communitySlug, joinRequestId, now));
        }

        if (rows.Count == 0)
            return;

        _notifications.AddRange(rows);
        await _notifications.SaveChangesAsync(cancellationToken);
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
