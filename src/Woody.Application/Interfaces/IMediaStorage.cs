namespace Woody.Application.Interfaces;

public sealed record StoredMediaFile(string StorageKey, string ContentType, long SizeBytes);

public sealed record MediaReadResult(Stream Content, string ContentType, long SizeBytes);

public interface IMediaStorage
{
    Task<StoredMediaFile> SaveImageAsync(
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<StoredMediaFile> SaveVideoAsync(
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<MediaReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
