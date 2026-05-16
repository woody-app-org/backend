namespace Woody.Application.Media;

/// <summary>Encoda cada segmento da object key para uso em path de URL pública.</summary>
public static class MediaStorageUrlEncoding
{
    public static string EncodeKeyForUrlPath(string storageKey) =>
        string.Join("/", storageKey
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.EscapeDataString));
}
