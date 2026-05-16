using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Media;

/// <summary>Serialização estável na API (<c>image</c>, <c>video</c>, …).</summary>
public static class MediaKindApi
{
    public const string Image = "image";
    public const string Video = "video";
    public const string Gif = "gif";
    public const string Sticker = "sticker";

    public static bool TryParse(string? raw, out MediaKind kind)
    {
        kind = MediaKind.Image;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        switch (raw.Trim().ToLowerInvariant())
        {
            case Image:
                kind = MediaKind.Image;
                return true;
            case Video:
                kind = MediaKind.Video;
                return true;
            case Gif:
                kind = MediaKind.Gif;
                return true;
            case Sticker:
                kind = MediaKind.Sticker;
                return true;
            default:
                return false;
        }
    }

    public static string ToApiString(MediaKind kind) =>
        kind switch
        {
            MediaKind.Image => Image,
            MediaKind.Video => Video,
            MediaKind.Gif => Gif,
            MediaKind.Sticker => Sticker,
            _ => Image
        };

    public static MediaKind FromMimeForUploadedFile(string contentType, string extension)
    {
        var ct = contentType.Trim().ToLowerInvariant();
        var ext = extension.Trim().ToLowerInvariant();
        if (ext == ".gif" || ct == "image/gif")
            return MediaKind.Gif;
        if (ct.StartsWith("video/", StringComparison.Ordinal))
            return MediaKind.Video;
        return MediaKind.Image;
    }
}
