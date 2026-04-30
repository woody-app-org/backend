using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;
using Woody.Application.Media;
using Woody.Domain.Media;

namespace Woody.Application.Services;

public sealed class MediaUploadApplicationService : IMediaUploadApplicationService
{
    private readonly IMediaUploadService _uploads;
    private readonly ICommunityPermissionService _communityPermissions;
    private readonly IConversationRepository _conversations;
    private readonly MediaStorageOptions _options;

    public MediaUploadApplicationService(
        IMediaUploadService uploads,
        ICommunityPermissionService communityPermissions,
        IConversationRepository conversations,
        IOptions<MediaStorageOptions> options)
    {
        _uploads = uploads;
        _communityPermissions = communityPermissions;
        _conversations = conversations;
        _options = options.Value;
    }

    public async Task<MediaUploadResponseDto> UploadImageAsync(
        MediaUploadAuthorizationContext authorization,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthorizedAsync(authorization, isVideo: false, cancellationToken).ConfigureAwait(false);
        var maxBytes = _options.MaxImageSizeBytes;
        return await _uploads
            .UploadImageAsync(content, originalFileName, contentType, sizeBytes, maxBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MediaUploadResponseDto> UploadVideoAsync(
        MediaUploadAuthorizationContext authorization,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthorizedAsync(authorization, isVideo: true, cancellationToken).ConfigureAwait(false);

        var (maxBytes, maxDur) = authorization.Scope switch
        {
            MediaUploadScope.Post => (_options.MaxPostVideoUploadBytes, MediaReferenceConstraints.PostVideoMaxDeclaredSeconds),
            MediaUploadScope.Message => (_options.MaxMessageVideoUploadBytes, MediaReferenceConstraints.MessageVideoMaxDeclaredSeconds),
            _ => throw new ArgumentException("Escopo de upload inválido.")
        };

        return await _uploads.UploadVideoAsync(
                content,
                originalFileName,
                contentType,
                sizeBytes,
                maxBytes,
                maxDur,
                authorization.DeclaredDurationSeconds,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureAuthorizedAsync(
        MediaUploadAuthorizationContext authorization,
        bool isVideo,
        CancellationToken cancellationToken)
    {
        if (authorization.UserId <= 0)
            throw new MediaUploadForbiddenException("Sessão inválida.");

        if (isVideo && authorization.DeclaredDurationSeconds is int d && d < 0)
            throw new ArgumentException("durationSeconds não pode ser negativo.");

        switch (authorization.Scope)
        {
            case MediaUploadScope.Post:
                await EnsurePostComposerAsync(authorization, cancellationToken).ConfigureAwait(false);
                break;
            case MediaUploadScope.Message:
                await EnsureMessageComposerAsync(authorization, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentException("Escopo de upload inválido.");
        }
    }

    private async Task EnsurePostComposerAsync(
        MediaUploadAuthorizationContext authorization,
        CancellationToken cancellationToken)
    {
        var raw = (authorization.PublicationContext ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("Indique publicationContext: \"profile\" ou \"community\" para upload de post.");

        if (raw == "profile")
        {
            if (authorization.CommunityId is > 0)
                throw new ArgumentException("publicationContext \"profile\" não deve incluir communityId.");
            return;
        }

        if (raw != "community")
            throw new ArgumentException("publicationContext inválido. Use \"profile\" ou \"community\".");

        if (authorization.CommunityId is not int cid || cid <= 0)
            throw new ArgumentException("communityId é obrigatório quando publicationContext é \"community\".");

        var ok = await _communityPermissions
            .CanPublishPostAsync(cid, authorization.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
            throw new MediaUploadForbiddenException("Não tens permissão para anexar mídia nesta comunidade.");
    }

    private async Task EnsureMessageComposerAsync(
        MediaUploadAuthorizationContext authorization,
        CancellationToken cancellationToken)
    {
        if (authorization.ConversationId is not int cid || cid <= 0)
            throw new ArgumentException("conversationId é obrigatório para upload de mensagem.");

        var ok = await _conversations
            .IsParticipantAsync(cid, authorization.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (!ok)
            throw new MediaUploadForbiddenException("Não participas nesta conversa.");
    }
}
