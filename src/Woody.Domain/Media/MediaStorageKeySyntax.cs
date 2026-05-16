namespace Woody.Domain.Media;

/// <summary>
/// Validação centralizada de chaves de armazenamento geradas pelo servidor (local ou bucket).
/// Suporta chave legada plana e layout hierárquico <c>posts/{userId}/…</c> / <c>messages/{conversationId}/…</c>.
/// </summary>
public static class MediaStorageKeySyntax
{
    /// <summary>Indica se a chave é segura para resolver caminho em disco ou object key em S3.</summary>
    public static bool IsSafeServerMediaStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return false;

        if (storageKey.Contains('\\')
            || storageKey.Contains("..", StringComparison.Ordinal)
            || storageKey.StartsWith("/", StringComparison.Ordinal)
            || storageKey.EndsWith("/", StringComparison.Ordinal))
            return false;

        if (IsLegacyFlatHexFileKey(storageKey))
            return true;

        return IsStructuredPostOrMessageKey(storageKey);
    }

    public static bool IsPermittedImageStorageKeyForLocalApi(string storageKey) =>
        IsSafeServerMediaStorageKey(storageKey)
        && UploadedImagePolicy.GetContentTypeForStorageKey(storageKey) != null;

    public static bool IsPermittedVideoStorageKeyForLocalApi(string storageKey) =>
        IsSafeServerMediaStorageKey(storageKey)
        && UploadedVideoPolicy.GetContentTypeForStorageKey(storageKey) != null;

    private static bool IsStructuredPostOrMessageKey(string storageKey)
    {
        var parts = storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
            return false;

        var scope = parts[0];
        if (!string.Equals(scope, "posts", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(scope, "messages", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!int.TryParse(parts[1], out var segmentId) || segmentId <= 0)
            return false;

        return IsLegacyFlatHexFileKey(parts[2]);
    }

    private static bool IsLegacyFlatHexFileKey(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            return false;

        var name = Path.GetFileNameWithoutExtension(fileName);
        return name.Length == 32 && name.All(Uri.IsHexDigit);
    }
}
