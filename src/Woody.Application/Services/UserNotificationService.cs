using System.Text.Json;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Notifications;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IUserNotificationRepository _notifications;

    public UserNotificationService(IUserNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<UserNotificationListResponseDto> ListMineAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _notifications.ListForRecipientPagedAsync(recipientUserId, page, pageSize, cancellationToken);
        var dtos = items.Select(n =>
        {
            JsonElement payloadEl;
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(n.PayloadJson) ? "{}" : n.PayloadJson);
                payloadEl = doc.RootElement.Clone();
            }
            catch
            {
                using var doc = JsonDocument.Parse("{}");
                payloadEl = doc.RootElement.Clone();
            }

            return new UserNotificationItemDto
            {
                Id = n.Id.ToString(),
                Type = n.Type,
                CreatedAtUtc = n.CreatedAtUtc,
                ReadAtUtc = n.ReadAtUtc,
                Actor = n.ActorUser != null ? EntityMappers.ToUserPublicDto(n.ActorUser) : null,
                Payload = payloadEl
            };
        }).ToList();

        return new UserNotificationListResponseDto
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
        if (row.ReadAtUtc == null)
        {
            row.ReadAtUtc = DateTime.UtcNow;
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

        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload { PostId = postId },
            JsonOptions);
        _notifications.Add(new UserNotification
        {
            RecipientUserId = postOwnerUserId,
            ActorUserId = actorUserId,
            Type = UserNotificationTypeCodes.PostLike,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyPostCommentAsync(int actorUserId, int postOwnerUserId, int postId, int commentId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == postOwnerUserId)
            return;

        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload { PostId = postId, CommentId = commentId },
            JsonOptions);
        _notifications.Add(new UserNotification
        {
            RecipientUserId = postOwnerUserId,
            ActorUserId = actorUserId,
            Type = UserNotificationTypeCodes.PostComment,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
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

        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload
            {
                PostId = postId,
                ParentCommentId = parentCommentId,
                CommentId = replyCommentId
            },
            JsonOptions);
        _notifications.Add(new UserNotification
        {
            RecipientUserId = parentCommentAuthorUserId,
            ActorUserId = actorUserId,
            Type = UserNotificationTypeCodes.CommentReply,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyNewFollowerAsync(int actorUserId, int followedUserId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == followedUserId)
            return;

        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload { ProfileUserId = actorUserId },
            JsonOptions);
        _notifications.Add(new UserNotification
        {
            RecipientUserId = followedUserId,
            ActorUserId = actorUserId,
            Type = UserNotificationTypeCodes.NewFollower,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
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

        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload
            {
                ProfileUserId = senderUserId,
                ProfileSignalId = profileSignalId,
                ProfileSignalType = profileSignalTypeApi
            },
            JsonOptions);
        _notifications.Add(new UserNotification
        {
            RecipientUserId = receiverUserId,
            ActorUserId = senderUserId,
            Type = UserNotificationTypeCodes.ProfileSignal,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _notifications.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyMessageRequestAsync(int initiatorUserId, int recipientUserId, int conversationId, CancellationToken cancellationToken = default)
    {
        if (initiatorUserId == recipientUserId)
            return;

        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload
            {
                ConversationId = conversationId,
                ProfileUserId = initiatorUserId
            },
            JsonOptions);
        _notifications.Add(new UserNotification
        {
            RecipientUserId = recipientUserId,
            ActorUserId = initiatorUserId,
            Type = UserNotificationTypeCodes.MessageRequest,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
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
        var rows = new List<UserNotification>();
        foreach (var modId in moderatorUserIds.Distinct())
        {
            if (modId == requesterUserId)
                continue;

            var payload = JsonSerializer.Serialize(
                new UserNotificationNavigationPayload
                {
                    CommunityId = communityId,
                    CommunitySlug = communitySlug,
                    JoinRequestId = joinRequestId,
                    ProfileUserId = requesterUserId
                },
                JsonOptions);
            rows.Add(new UserNotification
            {
                RecipientUserId = modId,
                ActorUserId = requesterUserId,
                Type = UserNotificationTypeCodes.CommunityRequest,
                PayloadJson = payload,
                CreatedAtUtc = now
            });
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
        var payload = JsonSerializer.Serialize(
            new UserNotificationNavigationPayload
            {
                CommunityId = communityId,
                CommunitySlug = communitySlug,
                JoinRequestId = joinRequestId
            },
            JsonOptions);
        int? actorId = approverUserId is > 0 and var aid && aid != applicantUserId ? aid : null;
        _notifications.Add(new UserNotification
        {
            RecipientUserId = applicantUserId,
            ActorUserId = actorId,
            Type = UserNotificationTypeCodes.CommunityRequestApproved,
            PayloadJson = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _notifications.SaveChangesAsync(cancellationToken);
    }
}
