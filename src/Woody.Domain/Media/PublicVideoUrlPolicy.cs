using System.Net;
using System.Net.Sockets;

namespace Woody.Domain.Media;

/// <summary>URLs de vídeo públicas (HTTPS ou ficheiros servidos pela API Woody).</summary>
public static class PublicVideoUrlPolicy
{
    public const string LocalVideoMediaPathPrefix = "/api/media/videos/";

    public static bool IsPermittedVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        return IsPermittedLocalVideoPath(trimmed) || IsPermittedExternalVideoUrl(trimmed);
    }

    private static bool IsPermittedLocalVideoPath(string url)
    {
        if (url.Length > PublicImageUrlPolicy.MaxUrlLength || url.Any(char.IsControl))
            return false;

        if (!url.StartsWith(LocalVideoMediaPathPrefix, StringComparison.Ordinal))
            return false;

        if (url.Contains('?') || url.Contains('#'))
            return false;

        var storageKey = url[LocalVideoMediaPathPrefix.Length..];
        return MediaStorageKeySyntax.IsPermittedVideoStorageKeyForLocalApi(storageKey);
    }

    private static bool IsPermittedExternalVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        if (trimmed.Length is < 12 or > PublicImageUrlPolicy.MaxUrlLength)
            return false;

        if (trimmed.Any(char.IsControl))
            return false;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(uri.Host))
            return false;

        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IsPrivateOrLocalAddress(uri.Host))
            return false;

        var path = uri.AbsolutePath;
        return path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivateOrLocalAddress(string host)
    {
        if (!IPAddress.TryParse(host, out var address))
            return false;

        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                   || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                   || (bytes[0] == 192 && bytes[1] == 168)
                   || (bytes[0] == 169 && bytes[1] == 254);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                   || address.IsIPv6SiteLocal
                   || address.Equals(IPAddress.IPv6Loopback);
        }

        return false;
    }
}
