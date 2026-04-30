using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Media;
using Woody.Domain.Media;

namespace Woody.Application.Services;

public class MediaUploadService : IMediaUploadService
{
    private const int MagicBytesToRead = 24;

    private readonly IMediaStorageProvider _storage;

    public MediaUploadService(IMediaStorageProvider storage)
    {
        _storage = storage;
    }

    public async Task<MediaUploadResponseDto> UploadImageAsync(
        MediaStorageWriteContext storageContext,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        long maxSizeBytes,
        CancellationToken cancellationToken = default)
    {
        var metadata = UploadedImagePolicy.ValidateMetadata(
            originalFileName,
            contentType,
            sizeBytes,
            maxSizeBytes);

        if (!metadata.IsValid)
            throw new ArgumentException(metadata.Error);

        await using var buffered = await BufferStreamAsync(content, sizeBytes, maxSizeBytes, cancellationToken);

        var headerLength = (int)Math.Min(MagicBytesToRead, buffered.Length);
        var header = new byte[headerLength];
        buffered.Position = 0;
        var read = await buffered.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        if (!UploadedImagePolicy.HasValidMagicBytes(header.AsSpan(0, read), metadata.Extension!))
            throw new ArgumentException("Assinatura do arquivo inválida.");

        buffered.Position = 0;
        var stored = await _storage
            .PutImageAsync(storageContext, buffered, metadata.Extension!, metadata.ContentType!, cancellationToken)
            .ConfigureAwait(false);

        var kind = MediaKindApi.FromMimeForUploadedFile(metadata.ContentType!, metadata.Extension!);
        return new MediaUploadResponseDto
        {
            Url = _storage.BuildPublicImageUrl(stored.StorageKey),
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            MediaKind = MediaKindApi.ToApiString(kind)
        };
    }

    public async Task<MediaUploadResponseDto> UploadVideoAsync(
        MediaStorageWriteContext storageContext,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        long maxSizeBytes,
        int? maxDeclaredDurationSeconds,
        int? clientDeclaredDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        var metadata = UploadedVideoPolicy.ValidateMetadata(
            originalFileName,
            contentType,
            sizeBytes,
            maxSizeBytes,
            maxDeclaredDurationSeconds,
            clientDeclaredDurationSeconds);

        if (!metadata.IsValid)
            throw new ArgumentException(metadata.Error);

        await using var buffered = await BufferStreamAsync(content, sizeBytes, maxSizeBytes, cancellationToken);

        var headerLength = (int)Math.Min(MagicBytesToRead, buffered.Length);
        var header = new byte[headerLength];
        buffered.Position = 0;
        var read = await buffered.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        if (!UploadedVideoPolicy.HasValidMagicBytes(header.AsSpan(0, read), metadata.Extension!))
            throw new ArgumentException("Assinatura de vídeo inválida.");

        buffered.Position = 0;
        var stored = await _storage
            .PutVideoAsync(storageContext, buffered, metadata.Extension!, metadata.ContentType!, cancellationToken)
            .ConfigureAwait(false);

        int? durationMs = clientDeclaredDurationSeconds is int s ? s * 1000 : null;

        return new MediaUploadResponseDto
        {
            Url = _storage.BuildPublicVideoUrl(stored.StorageKey),
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            MediaKind = MediaKindApi.Video,
            DurationMs = durationMs,
            DurationSeconds = clientDeclaredDurationSeconds
        };
    }

    private static async Task<MemoryStream> BufferStreamAsync(
        Stream content,
        long declaredSizeBytes,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (declaredSizeBytes > maxBytes)
            throw new ArgumentException("Arquivo inválido.");

        var capacity = (int)Math.Min(declaredSizeBytes, int.MaxValue);
        var buffered = new MemoryStream(capacity: Math.Max(capacity, 256));
        var buffer = new byte[64 * 1024];
        long totalRead = 0;
        while (totalRead < declaredSizeBytes)
        {
            var toRead = (int)Math.Min(buffer.Length, declaredSizeBytes - totalRead);
            var read = await content.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);
            if (read == 0)
                break;
            totalRead += read;
            if (totalRead > maxBytes)
                throw new ArgumentException("Arquivo inválido.");
            await buffered.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (buffered.Length != declaredSizeBytes)
            throw new ArgumentException("Arquivo inválido.");

        return buffered;
    }
}
