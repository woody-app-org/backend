namespace Woody.Application.Interfaces;

public sealed record StoredMediaFile(string StorageKey, string ContentType, long SizeBytes);

public sealed record MediaReadResult(Stream Content, string ContentType, long SizeBytes);

/// <summary>
/// Abstração de armazenamento de blobs multimédia. Implementação actual: <c>LocalMediaStorage</c>;
/// futuro: provider compatível com S3/R2 (mesmo contrato de chave + content-type).
/// </summary>
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
