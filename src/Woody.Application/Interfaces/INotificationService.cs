using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponseDto> ListMineAsync(
        int recipientUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(int recipientUserId, CancellationToken cancellationToken = default);

    Task<bool> TryMarkReadAsync(int recipientUserId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(int recipientUserId, CancellationToken cancellationToken = default);

    Task NotifyPostLikedAsync(int actorUserId, int postOwnerUserId, int postId, CancellationToken cancellationToken = default);

    Task NotifyPostCommentAsync(int actorUserId, int postOwnerUserId, int postId, int commentId, CancellationToken cancellationToken = default);

    Task NotifyCommentReplyAsync(
        int actorUserId,
        int parentCommentAuthorUserId,
        int postId,
        int parentCommentId,
        int replyCommentId,
        CancellationToken cancellationToken = default);

    Task NotifyNewFollowerAsync(int actorUserId, int followedUserId, CancellationToken cancellationToken = default);

    Task NotifyProfileSignalAsync(
        int senderUserId,
        int receiverUserId,
        int profileSignalId,
        string profileSignalTypeApi,
        CancellationToken cancellationToken = default);

    Task NotifyMessageRequestAsync(int initiatorUserId, int recipientUserId, int conversationId, CancellationToken cancellationToken = default);

    Task NotifyCommunityJoinRequestAsync(
        int requesterUserId,
        int communityId,
        string communitySlug,
        int joinRequestId,
        IReadOnlyList<int> moderatorUserIds,
        CancellationToken cancellationToken = default);

    Task NotifyCommunityRequestApprovedAsync(
        int applicantUserId,
        int? approverUserId,
        int communityId,
        string communitySlug,
        int joinRequestId,
        CancellationToken cancellationToken = default);
}
