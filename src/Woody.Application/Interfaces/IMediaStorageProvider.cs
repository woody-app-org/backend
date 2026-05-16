using Woody.Application.Media;

namespace Woody.Application.Interfaces;

public sealed record StoredMediaFile(string StorageKey, string ContentType, long SizeBytes);

public sealed record MediaReadResult(Stream Content, string ContentType, long SizeBytes);

/// <summary>
/// Abstração de armazenamento de blobs (local, S3, R2). A chave devolvida em <see cref="StoredMediaFile.StorageKey"/>
/// é a fonte de verdade para <c>MediaAttachment.StorageKey</c> e URLs públicas.
/// </summary>
public interface IMediaStorageProvider
{
    Task<StoredMediaFile> PutImageAsync(
        MediaStorageWriteContext context,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<StoredMediaFile> PutVideoAsync(
        MediaStorageWriteContext context,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> TryDeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<MediaReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    string BuildPublicImageUrl(string storageKey);

    string BuildPublicVideoUrl(string storageKey);

    /// <summary>
    /// URL assinada de leitura (ex.: bucket privado). Implementações locais devolvem null.
    /// </summary>
    string? TryCreatePresignedGetUrl(string storageKey, bool isVideo, TimeSpan lifetime);
}
