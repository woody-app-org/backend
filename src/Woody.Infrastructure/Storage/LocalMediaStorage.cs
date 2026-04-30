using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Domain.Media;

namespace Woody.Infrastructure.Storage;

public class LocalMediaStorage : IMediaStorage
{
    private readonly MediaStorageOptions _options;

    public LocalMediaStorage(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredMediaFile> SaveImageAsync(
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (UploadedImagePolicy.GetContentTypeForStorageKey($"file{normalizedExtension}") != contentType)
            throw new ArgumentException("Tipo de arquivo inválido.");

        return await SaveBlobAsync(content, normalizedExtension, contentType, cancellationToken);
    }

    public async Task<StoredMediaFile> SaveVideoAsync(
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (UploadedVideoPolicy.GetContentTypeForStorageKey($"file{normalizedExtension}") != contentType)
            throw new ArgumentException("Tipo de arquivo inválido.");

        return await SaveBlobAsync(content, normalizedExtension, contentType, cancellationToken);
    }

    private static string NormalizeExtension(string extension)
    {
        var e = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        return e;
    }

    private async Task<StoredMediaFile> SaveBlobAsync(
        Stream content,
        string normalizedExtension,
        string contentType,
        CancellationToken cancellationToken)
    {
        var storageKey = $"{Guid.NewGuid():N}{normalizedExtension}";
        var fullPath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var destination = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);
        await content.CopyToAsync(destination, cancellationToken);

        return new StoredMediaFile(storageKey, contentType, destination.Length);
    }

    public Task<MediaReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsServerGeneratedStorageKey(storageKey))
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

    private string ResolvePath(string storageKey)
    {
        var root = GetRootPath();
        var fullPath = Path.GetFullPath(Path.Combine(root, storageKey));
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

    private static bool IsServerGeneratedStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return false;

        var fileName = Path.GetFileName(storageKey);
        if (!string.Equals(storageKey, fileName, StringComparison.Ordinal))
            return false;

        var name = Path.GetFileNameWithoutExtension(fileName);
        return name.Length == 32 && name.All(Uri.IsHexDigit);
    }
}
