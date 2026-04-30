using System.Diagnostics.CodeAnalysis;

namespace Woody.Domain.Media;

/// <summary>
/// Extrai a <c>storageKey</c> a partir de URLs públicas servidas pela API Woody (<c>/api/media/images/…</c>, <c>/api/media/videos/…</c>).
/// </summary>
public static class LocalMediaUrlParser
{
    public static bool TryGetImageStorageKeyFromLocalUrl(string url, [NotNullWhen(true)] out string? storageKey) =>
        TryGetKey(
            url,
            PublicImageUrlPolicy.LocalMediaPathPrefix,
            MediaStorageKeySyntax.IsPermittedImageStorageKeyForLocalApi,
            out storageKey);

    public static bool TryGetVideoStorageKeyFromLocalUrl(string url, [NotNullWhen(true)] out string? storageKey) =>
        TryGetKey(
            url,
            PublicVideoUrlPolicy.LocalVideoMediaPathPrefix,
            MediaStorageKeySyntax.IsPermittedVideoStorageKeyForLocalApi,
            out storageKey);

    private static bool TryGetKey(
        string url,
        string prefix,
        Func<string, bool> isValidKey,
        [NotNullWhen(true)] out string? storageKey)
    {
        storageKey = null;
        var t = (url ?? string.Empty).Trim();
        if (t.Length == 0 || !t.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var tail = t[prefix.Length..];
        if (tail.Length == 0 || tail.Contains('?', StringComparison.Ordinal) || tail.Contains('#', StringComparison.Ordinal))
            return false;

        var segments = tail.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            return false;

        var key = string.Join("/", segments.Select(Uri.UnescapeDataString));
        if (!isValidKey(key))
            return false;

        storageKey = key;
        return true;
    }
}
