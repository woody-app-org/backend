using Woody.Application.DTOs;

using Woody.Application.Media;

namespace Woody.Application.Interfaces;

/// <summary>
/// Persistência e validação técnica (MIME, extensão, assinatura, tamanho) do ficheiro.
/// Permissões de negócio ficam em <see cref="IMediaUploadApplicationService"/>.
/// </summary>
public interface IMediaUploadService
{
    Task<MediaUploadResponseDto> UploadImageAsync(
        MediaStorageWriteContext storageContext,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        long maxSizeBytes,
        CancellationToken cancellationToken = default);

    Task<MediaUploadResponseDto> UploadVideoAsync(
        MediaStorageWriteContext storageContext,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        long maxSizeBytes,
        int? maxDeclaredDurationSeconds,
        int? clientDeclaredDurationSeconds,
        CancellationToken cancellationToken = default);
}
