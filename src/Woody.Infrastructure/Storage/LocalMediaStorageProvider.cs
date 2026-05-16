using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Media;
using Woody.Domain.Media;

namespace Woody.Infrastructure.Storage;

public sealed class LocalMediaStorageProvider : IMediaStorageProvider
{
    private readonly MediaStorageOptions _options;

    public LocalMediaStorageProvider(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredMediaFile> PutImageAsync(
        MediaStorageWriteContext context,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (UploadedImagePolicy.GetContentTypeForStorageKey($"file{normalizedExtension}") != contentType)
            throw new ArgumentException("Tipo de arquivo inválido.");

        return await SaveBlobAsync(context, content, normalizedExtension, contentType, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<StoredMediaFile> PutVideoAsync(
        MediaStorageWriteContext context,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (UploadedVideoPolicy.GetContentTypeForStorageKey($"file{normalizedExtension}") != contentType)
            throw new ArgumentException("Tipo de arquivo inválido.");

        return await SaveBlobAsync(context, content, normalizedExtension, contentType, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> TryDeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!MediaStorageKeySyntax.IsSafeServerMediaStorageKey(storageKey))
            return Task.FromResult(false);

        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<MediaReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!MediaStorageKeySyntax.IsSafeServerMediaStorageKey(storageKey))
            return Task.FromResult<MediaReadResult?>(null);

        var contentType = AttachmentStorageCatalog.GetContentTypeForStorageKey(storageKey);
        if (contentType == null)
            return Task.FromResult<MediaReadResult?>(null);

        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<MediaReadResult?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        var result = new MediaReadResult(stream, contentType, new FileInfo(fullPath).Length);
        return Task.FromResult<MediaReadResult?>(result);
    }

    public string BuildPublicImageUrl(string storageKey) =>
        BuildPublicUrl(
            string.IsNullOrWhiteSpace(_options.PublicUrlPath)
                ? "/api/media/images"
                : _options.PublicUrlPath.TrimEnd('/'),
            storageKey);

    public string BuildPublicVideoUrl(string storageKey) =>
        BuildPublicUrl(
            string.IsNullOrWhiteSpace(_options.PublicVideoUrlPath)
                ? "/api/media/videos"
                : _options.PublicVideoUrlPath.TrimEnd('/'),
            storageKey);

    public string? TryCreatePresignedGetUrl(string storageKey, bool isVideo, TimeSpan lifetime) => null;

    private static string BuildPublicUrl(string basePath, string storageKey) =>
        $"{basePath}/{MediaStorageUrlEncoding.EncodeKeyForUrlPath(storageKey)}";

    private static string NormalizeExtension(string extension)
    {
        var e = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        return e;
    }

    private async Task<StoredMediaFile> SaveBlobAsync(
        MediaStorageWriteContext context,
        Stream content,
        string normalizedExtension,
        string contentType,
        CancellationToken cancellationToken)
    {
        var prefix = context.ObjectKeyPrefix.Trim().Replace('\\', '/');
        if (prefix.Length == 0 || !prefix.EndsWith('/'))
            throw new ArgumentException("Prefixo de object key inválido.");

        var storageKey = $"{prefix}{Guid.NewGuid():N}{normalizedExtension}";
        var fullPath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var destination = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);
        await content.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        return new StoredMediaFile(storageKey, contentType, destination.Length);
    }

    private string ResolvePath(string storageKey)
    {
        var root = GetRootPath();
        var normalized = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        var relative = Path.GetRelativePath(root, fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new InvalidOperationException("Storage key inválida.");
        return fullPath;
    }

    private string GetRootPath()
    {
        var configured = string.IsNullOrWhiteSpace(_options.RootPath)
            ? "App_Data/media"
            : _options.RootPath.Trim();

        var root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);

        return Path.GetFullPath(root);
    }
}
