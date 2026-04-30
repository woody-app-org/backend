using Woody.Application.DTOs;

namespace Woody.Application.Interfaces;

public interface IMediaUploadService
{
    Task<MediaUploadResponseDto> UploadImageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<MediaUploadResponseDto> UploadVideoAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);
}
