using Woody.Application.DTOs.Api;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Messaging;

namespace Woody.Application.Services;

public sealed class DirectMessagingService : IDirectMessagingService
{
    private readonly IConversationRepository _conversations;
    private readonly IFollowRepository _follows;
    private readonly IUserRepository _users;

    public DirectMessagingService(
        IConversationRepository conversations,
        IFollowRepository follows,
        IUserRepository users)
    {
        _conversations = conversations;
        _follows = follows;
        _users = users;
    }

    public async Task<ConversationResponseDto> StartOrGetConversationAsync(
        int actorUserId,
        int otherUserId,
        CancellationToken cancellationToken = default)
    {
        if (otherUserId <= 0)
            throw new ArgumentException("Identificador da outra utilizadora inválido.", nameof(otherUserId));

        var (low, high) = DirectMessageConversationPolicy.OrderParticipantPair(actorUserId, otherUserId);

        if (await _users.GetByIdNoTrackingAsync(otherUserId, cancellationToken) == null)
            throw new KeyNotFoundException("Utilizadora não encontrada.");

        var mutual = await _follows.AreMutualFollowersAsync(actorUserId, otherUserId, cancellationToken);
        var existing = await _conversations.GetTrackedByPairAsync(low, high, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (existing != null)
        {
            var changed = false;

            if (existing.Status == ConversationStatus.Rejected)
            {
                if (mutual)
                {
                    existing.Status = ConversationStatus.Accepted;
                    existing.InitiatorUserId = null;
                }
                else
                {
                    existing.Status = ConversationStatus.Pending;
                    existing.InitiatorUserId = actorUserId;
                }

                existing.RespondedAt = null;
                existing.UpdatedAt = utcNow;
                changed = true;
            }
            else if (existing.Status == ConversationStatus.Pending && mutual)
            {
                existing.Status = ConversationStatus.Accepted;
                existing.InitiatorUserId = null;
                existing.UpdatedAt = utcNow;
                changed = true;
            }

            if (changed)
                await _conversations.SaveChangesAsync(cancellationToken);

            return ConversationDtoMapper.ToResponse(existing, actorUserId);
        }

        var status = DirectMessageConversationPolicy.InitialStatus(mutual);
        var initiator = DirectMessageConversationPolicy.InitialInitiatorUserId(mutual, actorUserId);

        var conversation = new Conversation
        {
            UserLowId = low,
            UserHighId = high,
            InitiatorUserId = initiator,
            Status = status,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Participants =
            {
                new ConversationParticipant { UserId = low, JoinedAt = utcNow },
                new ConversationParticipant { UserId = high, JoinedAt = utcNow }
            }
        };

        _conversations.Add(conversation);
        await _conversations.SaveChangesAsync(cancellationToken);

        var tracked = await _conversations.GetTrackedByPairAsync(low, high, cancellationToken)
                      ?? conversation;

        return ConversationDtoMapper.ToResponse(tracked, actorUserId);
    }

    public async Task<IReadOnlyList<ConversationResponseDto>> ListMyConversationsAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await _conversations.ListMineNoTrackingAsync(actorUserId, cancellationToken);
        return ConversationDtoMapper.ToResponseList(list, actorUserId);
    }

    public async Task<IReadOnlyList<ConversationResponseDto>> ListPendingRequestsReceivedAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await _conversations.ListPendingInboundNoTrackingAsync(actorUserId, cancellationToken);
        return ConversationDtoMapper.ToResponseList(list, actorUserId);
    }

    public async Task<ConversationResponseDto> GetConversationForParticipantAsync(
        int actorUserId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var c = await _conversations.GetTrackedByIdForParticipantAsync(conversationId, actorUserId, cancellationToken);
        if (c == null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        return ConversationDtoMapper.ToResponse(c, actorUserId);
    }

    public async Task<ConversationResponseDto> AcceptPendingAsync(
        int actorUserId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var c = await _conversations.GetTrackedByIdForParticipantAsync(conversationId, actorUserId, cancellationToken);
        if (c == null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        if (c.Status != ConversationStatus.Pending)
            throw new InvalidOperationException("Este pedido de conversa já não está pendente.");

        if (!DirectMessageConversationPolicy.MayAcceptOrRejectRequest(c, actorUserId))
            throw new ForbiddenException("Só a utilizadora que recebeu o pedido pode aceitar esta conversa.");

        c.Status = ConversationStatus.Accepted;
        c.InitiatorUserId = null;
        c.RespondedAt = DateTime.UtcNow;
        c.UpdatedAt = DateTime.UtcNow;

        await _conversations.SaveChangesAsync(cancellationToken);
        return ConversationDtoMapper.ToResponse(c, actorUserId);
    }

    public async Task<ConversationResponseDto> RejectPendingAsync(
        int actorUserId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var c = await _conversations.GetTrackedByIdForParticipantAsync(conversationId, actorUserId, cancellationToken);
        if (c == null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        if (c.Status != ConversationStatus.Pending)
            throw new InvalidOperationException("Este pedido de conversa já não está pendente.");

        if (!DirectMessageConversationPolicy.MayAcceptOrRejectRequest(c, actorUserId))
            throw new ForbiddenException("Só a utilizadora que recebeu o pedido pode recusar esta conversa.");

        c.Status = ConversationStatus.Rejected;
        c.RespondedAt = DateTime.UtcNow;
        c.UpdatedAt = DateTime.UtcNow;

        await _conversations.SaveChangesAsync(cancellationToken);
        return ConversationDtoMapper.ToResponse(c, actorUserId);
    }
}
