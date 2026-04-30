using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Domain.Media;

namespace Woody.Application.Services;

public class MediaUploadService : IMediaUploadService
{
    private const int MagicBytesToRead = 24;

    private readonly IMediaStorage _storage;
    private readonly MediaStorageOptions _options;

    public MediaUploadService(IMediaStorage storage, IOptions<MediaStorageOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    public async Task<MediaUploadResponseDto> UploadImageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var metadata = UploadedImagePolicy.ValidateMetadata(
            originalFileName,
            contentType,
            sizeBytes,
            _options.MaxImageSizeBytes);

        if (!metadata.IsValid)
            throw new ArgumentException(metadata.Error);

        await using var buffered = await BufferStreamAsync(content, sizeBytes, _options.MaxImageSizeBytes, cancellationToken);

        var headerLength = (int)Math.Min(MagicBytesToRead, buffered.Length);
        var header = new byte[headerLength];
        buffered.Position = 0;
        var read = await buffered.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        if (!UploadedImagePolicy.HasValidMagicBytes(header.AsSpan(0, read), metadata.Extension!))
            throw new ArgumentException("Assinatura do arquivo inválida.");

        buffered.Position = 0;
        var stored = await _storage.SaveImageAsync(
            buffered,
            metadata.Extension!,
            metadata.ContentType!,
            cancellationToken);

        var kind = MediaKindApi.FromMimeForUploadedFile(metadata.ContentType!, metadata.Extension!);
        return new MediaUploadResponseDto
        {
            Url = BuildImagePublicUrl(stored.StorageKey),
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            MediaKind = MediaKindApi.ToApiString(kind)
        };
    }

    public async Task<MediaUploadResponseDto> UploadVideoAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var metadata = UploadedVideoPolicy.ValidateMetadata(
            originalFileName,
            contentType,
            sizeBytes,
            _options.MaxVideoSizeBytes);

        if (!metadata.IsValid)
            throw new ArgumentException(metadata.Error);

        await using var buffered = await BufferStreamAsync(content, sizeBytes, _options.MaxVideoSizeBytes, cancellationToken);

        var headerLength = (int)Math.Min(MagicBytesToRead, buffered.Length);
        var header = new byte[headerLength];
        buffered.Position = 0;
        var read = await buffered.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        if (!UploadedVideoPolicy.HasValidMagicBytes(header.AsSpan(0, read), metadata.Extension!))
            throw new ArgumentException("Assinatura de vídeo inválida.");

        buffered.Position = 0;
        var stored = await _storage.SaveVideoAsync(
            buffered,
            metadata.Extension!,
            metadata.ContentType!,
            cancellationToken);

        return new MediaUploadResponseDto
        {
            Url = BuildVideoPublicUrl(stored.StorageKey),
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            MediaKind = MediaKindApi.Video
        };
    }

    private static async Task<MemoryStream> BufferStreamAsync(
        Stream content,
        long sizeBytes,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffered = new MemoryStream(capacity: (int)Math.Min(sizeBytes, maxBytes));
        await content.CopyToAsync(buffered, cancellationToken);

        if (buffered.Length != sizeBytes)
            throw new ArgumentException("Arquivo inválido.");

        return buffered;
    }

    private string BuildImagePublicUrl(string storageKey)
    {
        var basePath = string.IsNullOrWhiteSpace(_options.PublicUrlPath)
            ? "/api/media/images"
            : _options.PublicUrlPath.TrimEnd('/');
        return $"{basePath}/{Uri.EscapeDataString(storageKey)}";
    }

    private string BuildVideoPublicUrl(string storageKey)
    {
        var basePath = string.IsNullOrWhiteSpace(_options.PublicVideoUrlPath)
            ? "/api/media/videos"
            : _options.PublicVideoUrlPath.TrimEnd('/');
        return $"{basePath}/{Uri.EscapeDataString(storageKey)}";
    }
}
