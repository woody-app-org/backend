using Woody.Application.DTOs.Api;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Media;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;
using Woody.Domain.Messaging;

namespace Woody.Application.Services;

public sealed class DirectMessagingService : IDirectMessagingService
{
    private const int MaxMessageBodyLength = 16_000;
    private const int MaxMessageAttachments = 10;
    /// <summary>Permite data URLs de imagem (mesmo padrão conceptual dos posts) até ~450KB ficheiro.</summary>
    private const int MaxDataImageAttachmentUrlLength = 900_000;
    private const int MaxExternalAttachmentUrlLength = PublicImageUrlPolicy.MaxUrlLength;

    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IFollowRepository _follows;
    private readonly IUserRepository _users;
    private readonly IDirectMessageRealtimePublisher _realtime;
    private readonly INotificationService _notificationService;

    public DirectMessagingService(
        IConversationRepository conversations,
        IMessageRepository messages,
        IFollowRepository follows,
        IUserRepository users,
        IDirectMessageRealtimePublisher realtime,
        INotificationService notificationService)
    {
        _conversations = conversations;
        _messages = messages;
        _follows = follows;
        _users = users;
        _realtime = realtime;
        _notificationService = notificationService;
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
            var notifyMessageRequest = false;

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
                    notifyMessageRequest = true;
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

            if (notifyMessageRequest)
                await TryNotifyMessageRequestIfPendingAsync(existing, cancellationToken);

            var existingDto = ConversationDtoMapper.ToResponse(existing, actorUserId);
            await EnrichConversationPreviewsAsync(new[] { existingDto }, cancellationToken);
            return existingDto;
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

        if (tracked.Status == ConversationStatus.Pending)
            await TryNotifyMessageRequestIfPendingAsync(tracked, cancellationToken);

        var createdDto = ConversationDtoMapper.ToResponse(tracked, actorUserId);
        await EnrichConversationPreviewsAsync(new[] { createdDto }, cancellationToken);
        return createdDto;
    }

    public async Task<IReadOnlyList<ConversationResponseDto>> ListMyConversationsAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await _conversations.ListMineNoTrackingAsync(actorUserId, cancellationToken);
        var dtos = ConversationDtoMapper.ToResponseList(list, actorUserId).ToList();
        await EnrichConversationPreviewsAsync(dtos, cancellationToken);
        return dtos;
    }

    public async Task<IReadOnlyList<ConversationResponseDto>> ListPendingRequestsReceivedAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await _conversations.ListPendingInboundNoTrackingAsync(actorUserId, cancellationToken);
        var dtos = ConversationDtoMapper.ToResponseList(list, actorUserId).ToList();
        await EnrichConversationPreviewsAsync(dtos, cancellationToken);
        return dtos;
    }

    public async Task<ConversationResponseDto> GetConversationForParticipantAsync(
        int actorUserId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var c = await _conversations.GetTrackedByIdForParticipantAsync(conversationId, actorUserId, cancellationToken);
        if (c == null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        var dto = ConversationDtoMapper.ToResponse(c, actorUserId);
        await EnrichConversationPreviewsAsync(new[] { dto }, cancellationToken);
        return dto;
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
        var acceptedDto = ConversationDtoMapper.ToResponse(c, actorUserId);
        await _realtime.BroadcastConversationUpdatedAsync(
            ConversationDtoMapper.ToRealtime(c),
            c.UserLowId,
            c.UserHighId,
            cancellationToken);
        await EnrichConversationPreviewsAsync(new[] { acceptedDto }, cancellationToken);
        return acceptedDto;
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
        var convDto = ConversationDtoMapper.ToResponse(c, actorUserId);
        await _realtime.BroadcastConversationUpdatedAsync(
            ConversationDtoMapper.ToRealtime(c),
            c.UserLowId,
            c.UserHighId,
            cancellationToken);
        await EnrichConversationPreviewsAsync(new[] { convDto }, cancellationToken);
        return convDto;
    }

    public async Task<ConversationMessagesPageDto> ListMessagesAsync(
        int actorUserId,
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            throw new ArgumentException("Página inválida.", nameof(page));
        if (pageSize is < 1 or > 100)
            throw new ArgumentException("pageSize deve estar entre 1 e 100.", nameof(pageSize));

        if (!await _conversations.IsParticipantAsync(conversationId, actorUserId, cancellationToken))
            throw new KeyNotFoundException("Conversa não encontrada.");

        var (items, total) = await _messages.ListByConversationPagedAsync(
            conversationId,
            page,
            pageSize,
            cancellationToken);

        return new ConversationMessagesPageDto
        {
            Items = MessageDtoMapper.ToResponseList(items),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MessageResponseDto> SendMessageAsync(
        int actorUserId,
        int conversationId,
        SendConversationMessageRequestDto body,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetTrackedByIdForParticipantAsync(
            conversationId,
            actorUserId,
            cancellationToken);
        if (conversation == null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        if (!DirectMessageConversationPolicy.MaySendMessage(conversation, actorUserId))
            throw new ForbiddenException("Não podes enviar mensagens nesta conversa.");

        var text = string.IsNullOrWhiteSpace(body.Body) ? null : body.Body.Trim();
        var attachmentPlans = NormalizeIncomingAttachments(body);
        if (text == null && attachmentPlans.Count == 0)
            throw new ArgumentException("A mensagem precisa de texto ou de pelo menos um anexo.");

        if (text != null && text.Length > MaxMessageBodyLength)
            throw new ArgumentException($"O texto não pode exceder {MaxMessageBodyLength} caracteres.");

        if (attachmentPlans.Count > MaxMessageAttachments)
            throw new ArgumentException($"Máximo de {MaxMessageAttachments} anexos por mensagem.");

        var utcNow = DateTime.UtcNow;
        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = actorUserId,
            Body = text,
            CreatedAt = utcNow
        };

        conversation.UpdatedAt = utcNow;
        _messages.Add(message);
        await _messages.SaveChangesAsync(cancellationToken);

        var order = 0;
        foreach (var plan in attachmentPlans)
        {
            message.MediaAttachments.Add(
                new MediaAttachment
                {
                    OwnerType = MediaOwnerType.Message,
                    OwnerId = message.Id,
                    MessageId = message.Id,
                    Url = plan.Url,
                    DisplayOrder = order++,
                    CreatedAt = utcNow,
                    MediaKind = plan.Kind,
                    DurationMs = plan.Kind == MediaKind.Video && plan.DurationSeconds is int s ? s * 1000 : null,
                    Provider = plan.Provider,
                    ExternalId = plan.ExternalId,
                    ThumbnailUrl = plan.ThumbnailUrl,
                    StorageKey = plan.StorageKey,
                    MimeType = plan.MimeType,
                    FileSize = plan.FileSize
                });
        }

        if (attachmentPlans.Count > 0)
            await _messages.SaveChangesAsync(cancellationToken);

        var persisted = await _messages.GetNoTrackingByIdInConversationAsync(
            conversationId,
            message.Id,
            cancellationToken);
        if (persisted == null)
            throw new InvalidOperationException("Não foi possível carregar a mensagem após o envio.");

        var messageDto = MessageDtoMapper.ToResponse(persisted);
        await _realtime.BroadcastMessageCreatedAsync(
            messageDto,
            conversation.UserLowId,
            conversation.UserHighId,
            cancellationToken);
        return messageDto;
    }

    public async Task<MessageResponseDto> EditMessageAsync(
        int actorUserId,
        int conversationId,
        int messageId,
        EditConversationMessageRequestDto body,
        CancellationToken cancellationToken = default)
    {
        if (!await _conversations.IsParticipantAsync(conversationId, actorUserId, cancellationToken))
            throw new KeyNotFoundException("Mensagem não encontrada.");

        var message = await _messages.GetTrackedInConversationAsync(conversationId, messageId, cancellationToken);
        if (message == null)
            throw new KeyNotFoundException("Mensagem não encontrada.");

        if (message.DeletedAt != null)
            throw new KeyNotFoundException("Mensagem não encontrada.");

        if (!DirectMessageMessagePolicy.MayEditOrSoftDelete(message, actorUserId))
            throw new ForbiddenException("Só a autora pode editar esta mensagem.");

        var newBody = (body.Body ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(newBody))
            throw new ArgumentException("O texto da mensagem não pode ficar vazio.");

        if (newBody.Length > MaxMessageBodyLength)
            throw new ArgumentException($"O texto não pode exceder {MaxMessageBodyLength} caracteres.");

        message.Body = newBody;
        message.EditedAt = DateTime.UtcNow;

        var conv = await _conversations.GetTrackedByIdForParticipantAsync(conversationId, actorUserId, cancellationToken);
        if (conv != null)
            conv.UpdatedAt = DateTime.UtcNow;

        await _messages.SaveChangesAsync(cancellationToken);

        var persisted = await _messages.GetNoTrackingByIdInConversationAsync(
            conversationId,
            messageId,
            cancellationToken);
        if (persisted == null)
            throw new InvalidOperationException("Não foi possível carregar a mensagem após a edição.");

        var messageDto = MessageDtoMapper.ToResponse(persisted);
        var pair = await _conversations.GetParticipantPairIdsNoTrackingAsync(conversationId, cancellationToken);
        if (pair != null)
        {
            await _realtime.BroadcastMessageUpdatedAsync(
                messageDto,
                pair.Value.UserLowId,
                pair.Value.UserHighId,
                cancellationToken);
        }

        return messageDto;
    }

    public async Task DeleteMessageAsync(
        int actorUserId,
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        if (!await _conversations.IsParticipantAsync(conversationId, actorUserId, cancellationToken))
            throw new KeyNotFoundException("Mensagem não encontrada.");

        var message = await _messages.GetTrackedInConversationAsync(conversationId, messageId, cancellationToken);
        if (message == null)
            throw new KeyNotFoundException("Mensagem não encontrada.");

        if (message.DeletedAt != null)
        {
            if (message.SenderUserId != actorUserId)
                throw new KeyNotFoundException("Mensagem não encontrada.");
            return;
        }

        if (!DirectMessageMessagePolicy.MayEditOrSoftDelete(message, actorUserId))
            throw new ForbiddenException("Só a autora pode apagar esta mensagem.");

        var utcNow = DateTime.UtcNow;
        message.DeletedAt = utcNow;
        message.Body = null;
        message.EditedAt = null;

        if (message.MediaAttachments.Count > 0)
            _messages.RemoveMediaAttachments(message.MediaAttachments.ToList());

        var conv = await _conversations.GetTrackedByIdForParticipantAsync(conversationId, actorUserId, cancellationToken);
        if (conv != null)
            conv.UpdatedAt = utcNow;

        await _messages.SaveChangesAsync(cancellationToken);

        var deletedSnapshot = await _messages.GetNoTrackingByIdInConversationAsync(
            conversationId,
            messageId,
            cancellationToken);
        if (deletedSnapshot != null)
        {
            var dto = MessageDtoMapper.ToResponse(deletedSnapshot);
            var pair = await _conversations.GetParticipantPairIdsNoTrackingAsync(conversationId, cancellationToken);
            if (pair != null)
            {
                await _realtime.BroadcastMessageDeletedAsync(
                    dto,
                    pair.Value.UserLowId,
                    pair.Value.UserHighId,
                    cancellationToken);
            }
        }
    }

    private static int? RecipientUserIdForPendingConversation(Conversation c)
    {
        if (c.Status != ConversationStatus.Pending || c.InitiatorUserId is not int ini)
            return null;
        return c.UserLowId == ini ? c.UserHighId : c.UserLowId;
    }

    private async Task TryNotifyMessageRequestIfPendingAsync(Conversation c, CancellationToken cancellationToken = default)
    {
        if (c.Status != ConversationStatus.Pending || c.InitiatorUserId is not int initiator)
            return;
        var recipient = RecipientUserIdForPendingConversation(c);
        if (recipient is not int rid)
            return;
        await _notificationService.NotifyMessageRequestAsync(initiator, rid, c.Id, cancellationToken);
    }

    private async Task EnrichConversationPreviewsAsync(
        IReadOnlyList<ConversationResponseDto> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return;

        var ids = items.Select(i => i.Id).Distinct().ToList();
        var map = await _messages.GetLastMessageSummariesByConversationIdsAsync(ids, cancellationToken);
        foreach (var dto in items)
        {
            if (!map.TryGetValue(dto.Id, out var row))
                continue;
            dto.LastMessagePreview = row.Preview;
            dto.LastMessageAt = row.AtUtc;
        }
    }

    private sealed record AttachmentPlan(
        string Url,
        MediaKind Kind,
        int? DurationSeconds,
        string? Provider,
        string? ExternalId,
        string? ThumbnailUrl,
        string? StorageKey,
        string? MimeType,
        long? FileSize);

    private static List<AttachmentPlan> NormalizeIncomingAttachments(SendConversationMessageRequestDto body)
    {
        var list = new List<AttachmentPlan>();
        if (body.Attachments is { Count: > 0 })
        {
            foreach (var a in body.Attachments)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.Url))
                    throw new ArgumentException("Cada anexo precisa de URL.");

                if (!MediaKindApi.TryParse(a.MediaType, out var kind))
                    throw new ArgumentException("mediaType inválido. Use image, video, gif ou sticker.");

                if (a.DurationSeconds is int d &&
                    (d < 0 || d > MediaReferenceConstraints.MessageVideoMaxDeclaredSeconds))
                {
                    throw new ArgumentException(
                        $"durationSeconds inválido (0–{MediaReferenceConstraints.MessageVideoMaxDeclaredSeconds}).");
                }

                var t = a.Url.Trim();
                var isDataImage = t.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
                var maxLength = isDataImage ? MaxDataImageAttachmentUrlLength : MaxExternalAttachmentUrlLength;
                if (t.Length > maxLength)
                    throw new ArgumentException($"Cada URL de anexo não pode exceder {maxLength} caracteres.");

                if (!DirectMessageAttachmentPolicy.IsPermittedTypedAttachmentUrl(kind, t))
                    throw new ArgumentException("Anexo inválido para o tipo indicado.");

                if (!LocalAttachmentRequestMetadata.TryResolve(
                        kind,
                        t,
                        a.StorageKey,
                        a.MimeType,
                        a.FileSize,
                        MediaReferenceConstraints.MessageVideoMaxUploadBytes,
                        MediaReferenceConstraints.ImageMaxUploadBytes,
                        out var storageKey,
                        out var resolvedMime,
                        out var fileSize,
                        out var metaError))
                    throw new ArgumentException(metaError ?? "Metadados do anexo inválidos.");

                var thumb = TrimOrNull(a.ThumbnailUrl);
                if (thumb != null)
                {
                    if (thumb.Length > MaxExternalAttachmentUrlLength)
                        throw new ArgumentException($"thumbnailUrl não pode exceder {MaxExternalAttachmentUrlLength} caracteres.");
                    if (!PublicImageUrlPolicy.IsPermittedImageUrl(thumb))
                        throw new ArgumentException("thumbnailUrl inválida (HTTPS ou media local permitida).");
                }

                if (list.Any(x => x.Url == t))
                    continue;

                list.Add(new AttachmentPlan(
                    t,
                    kind,
                    a.DurationSeconds,
                    TrimOrNull(a.Provider),
                    TrimOrNull(a.ExternalId),
                    thumb,
                    storageKey,
                    resolvedMime,
                    fileSize));
                if (list.Count > MaxMessageAttachments)
                    throw new ArgumentException($"Máximo de {MaxMessageAttachments} anexos por mensagem.");
            }

            return list;
        }

        if (body.AttachmentUrls == null || body.AttachmentUrls.Count == 0)
            return list;

        foreach (var u in body.AttachmentUrls)
        {
            if (string.IsNullOrWhiteSpace(u))
                continue;
            var t = u.Trim();
            var isDataImage = t.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
            var maxLength = isDataImage ? MaxDataImageAttachmentUrlLength : MaxExternalAttachmentUrlLength;
            if (t.Length > maxLength)
                throw new ArgumentException($"Cada URL de anexo não pode exceder {maxLength} caracteres.");
            if (!DirectMessageAttachmentPolicy.IsPermittedAttachmentUrl(t))
                throw new ArgumentException(
                    "Cada anexo tem de ser uma imagem: URL https válida ou data:image (png, jpeg, gif ou webp) em base64.");
            if (list.Any(x => x.Url == t))
                continue;
            list.Add(new AttachmentPlan(t, MediaKind.Image, null, null, null, null, null, null, null));
            if (list.Count > MaxMessageAttachments)
                throw new ArgumentException($"Máximo de {MaxMessageAttachments} anexos por mensagem.");
        }

        return list;
    }

    private static string? TrimOrNull(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
}
