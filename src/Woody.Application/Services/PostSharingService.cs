using Woody.Application.DTOs.Api;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;

namespace Woody.Application.Services;

public sealed class PostSharingService : IPostSharingService
{
    private const string GenericShareError = "Não foi possível compartilhar esta publicação.";
    private const int MaxMessageBodyLength = 16_000;

    private readonly IPostRepository _posts;
    private readonly IResourceAuthorizationService _authorization;
    private readonly IUserRelationshipVisibilityService _visibility;
    private readonly IConversationRepository _conversations;
    private readonly IDirectMessagingService _directMessaging;
    private readonly INotificationService _notifications;

    public PostSharingService(
        IPostRepository posts,
        IResourceAuthorizationService authorization,
        IUserRelationshipVisibilityService visibility,
        IConversationRepository conversations,
        IDirectMessagingService directMessaging,
        INotificationService notifications)
    {
        _posts = posts;
        _authorization = authorization;
        _visibility = visibility;
        _conversations = conversations;
        _directMessaging = directMessaging;
        _notifications = notifications;
    }

    public async Task<SharePostToConversationResponseDto> ShareToConversationAsync(
        int actorUserId,
        int postId,
        SharePostToConversationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetByIdNonDeletedWithNavAsync(postId, cancellationToken);
        if (post == null || !await _authorization.CanReadPostAsync(post, actorUserId, cancellationToken))
            throw new ForbiddenException(GenericShareError);

        var text = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        if (text != null && text.Length > MaxMessageBodyLength)
            throw new ArgumentException($"O texto não pode exceder {MaxMessageBodyLength} caracteres.");

        int recipientUserId;
        int conversationId;

        if (request.ConversationId is int cid)
        {
            var conversation = await _conversations.GetTrackedByIdForParticipantAsync(
                cid,
                actorUserId,
                cancellationToken);
            if (conversation == null)
                throw new ForbiddenException(GenericShareError);

            recipientUserId = OtherParticipantId(conversation, actorUserId);
            conversationId = cid;
        }
        else if (request.RecipientUserId is int rid)
        {
            if (rid <= 0 || rid == actorUserId)
                throw new ForbiddenException(GenericShareError);

            var conversationDto = await _directMessaging.StartOrGetConversationAsync(
                actorUserId,
                rid,
                cancellationToken);
            recipientUserId = rid;
            conversationId = conversationDto.Id;
        }
        else
        {
            throw new ArgumentException("Indica recipientUserId ou conversationId.");
        }

        if (!await _authorization.CanReadPostAsync(post, recipientUserId, cancellationToken))
            throw new ForbiddenException(GenericShareError);

        if (await _visibility.AreUsersBlockedEitherWayAsync(actorUserId, recipientUserId, cancellationToken))
            throw new ForbiddenException(GenericShareError);

        var message = await _directMessaging.SendSharedPostMessageAsync(
            actorUserId,
            conversationId,
            postId,
            text,
            cancellationToken);

        await _notifications.NotifyPostSharedAsync(actorUserId, post.UserId, postId, cancellationToken);

        return new SharePostToConversationResponseDto
        {
            ConversationId = conversationId,
            Message = message
        };
    }

    private static int OtherParticipantId(Domain.Entities.Conversation conversation, int actorUserId) =>
        conversation.UserLowId == actorUserId ? conversation.UserHighId : conversation.UserLowId;
}
