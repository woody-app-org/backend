using Woody.Application.DTOs;
using Woody.Application.Media;

namespace Woody.Application.Interfaces;

/// <summary>
/// Orquestra upload multimédia com permissões por contexto (post vs mensagem).
/// O armazenamento concreto continua em <see cref="IMediaStorage"/> (local hoje, bucket depois).
/// </summary>
public interface IMediaUploadApplicationService
{
    Task<MediaUploadResponseDto> UploadImageAsync(
        MediaUploadAuthorizationContext authorization,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<MediaUploadResponseDto> UploadVideoAsync(
        MediaUploadAuthorizationContext authorization,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);
}
