using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Domain.Media;

namespace Woody.Application.Services;

public class MediaUploadService : IMediaUploadService
{
    private const int MagicBytesToRead = 12;

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

        await using var buffered = new MemoryStream(capacity: (int)Math.Min(sizeBytes, _options.MaxImageSizeBytes));
        await content.CopyToAsync(buffered, cancellationToken);

        if (buffered.Length != sizeBytes)
            throw new ArgumentException("Arquivo inválido.");

        buffered.Position = 0;
        var headerLength = (int)Math.Min(MagicBytesToRead, buffered.Length);
        var header = new byte[headerLength];
        var read = await buffered.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        if (!UploadedImagePolicy.HasValidMagicBytes(header.AsSpan(0, read), metadata.Extension!))
            throw new ArgumentException("Assinatura do arquivo inválida.");

        buffered.Position = 0;
        var stored = await _storage.SaveImageAsync(
            buffered,
            metadata.Extension!,
            metadata.ContentType!,
            cancellationToken);

        return new MediaUploadResponseDto
        {
            Url = BuildPublicUrl(stored.StorageKey),
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes
        };
    }

    private string BuildPublicUrl(string storageKey)
    {
        var basePath = string.IsNullOrWhiteSpace(_options.PublicUrlPath)
            ? "/api/media/images"
            : _options.PublicUrlPath.TrimEnd('/');
        return $"{basePath}/{Uri.EscapeDataString(storageKey)}";
    }
}
