using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;

namespace Woody.Infrastructure.Storage;

/// <summary>
/// Provider de storage privado local para documentos de verificação de identidade.
/// Salva arquivos em App_Data/verification (fora de wwwroot), nunca em rota pública.
/// Para produção, substituir por <c>S3PrivateVerificationDocumentStorageProvider</c>.
/// </summary>
public sealed class LocalPrivateVerificationDocumentStorageProvider : IVerificationDocumentStorageProvider
{
    private static readonly Dictionary<string, string> ContentTypeByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"]  = "image/png"
        };

    private readonly VerificationStorageOptions _options;

    public LocalPrivateVerificationDocumentStorageProvider(IOptions<VerificationStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(
        int userId,
        Stream content,
        string normalizedExtension,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new ArgumentException("userId inválido.", nameof(userId));

        if (!ContentTypeByExtension.ContainsKey(normalizedExtension))
            throw new ArgumentException("Extensão não permitida para documento.", nameof(normalizedExtension));

        var storageKey = $"verif/{userId}/{Guid.NewGuid():N}{normalizedExtension}";
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

        return storageKey;
    }

    public Task<VerificationDocumentReadResult?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidStorageKey(storageKey))
            return Task.FromResult<VerificationDocumentReadResult?>(null);

        var extension = Path.GetExtension(storageKey).ToLowerInvariant();
        if (!ContentTypeByExtension.TryGetValue(extension, out var contentType))
            return Task.FromResult<VerificationDocumentReadResult?>(null);

        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<VerificationDocumentReadResult?>(null);

        var info = new FileInfo(fullPath);
        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult<VerificationDocumentReadResult?>(
            new VerificationDocumentReadResult(stream, contentType, info.Length));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsValidStorageKey(storageKey))
            return Task.CompletedTask;

        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public bool IsValidStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return false;

        if (storageKey.Contains('\\')
            || storageKey.Contains("..", StringComparison.Ordinal)
            || storageKey.StartsWith("/", StringComparison.Ordinal))
            return false;

        // Esperado: verif/{userId}/{guid32}.{ext}
        var parts = storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        if (!string.Equals(parts[0], "verif", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!int.TryParse(parts[1], out var userId) || userId <= 0)
            return false;

        var fileName = parts[2];
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        if (name.Length != 32 || !name.All(Uri.IsHexDigit))
            return false;

        return ContentTypeByExtension.ContainsKey(ext);
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
            ? "App_Data/verification"
            : _options.RootPath.Trim();

        var root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);

        return Path.GetFullPath(root);
    }
}
