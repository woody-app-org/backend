using System.Net;
using System.Net.Sockets;

namespace Woody.Domain.Media;

public static class PublicImageUrlPolicy
{
    public const int MaxUrlLength = 2_048;
    public const string LocalMediaPathPrefix = "/api/media/images/";

    public static bool IsPermittedImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        return IsPermittedLocalMediaPath(trimmed) || IsPermittedExternalImageUrl(trimmed);
    }

    public static bool IsPermittedExternalImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        if (trimmed.Length is < 12 or > MaxUrlLength)
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

        return !IsPrivateOrLocalAddress(uri.Host);
    }

    private static bool IsPermittedLocalMediaPath(string url)
    {
        if (url.Length > MaxUrlLength || url.Any(char.IsControl))
            return false;

        if (!url.StartsWith(LocalMediaPathPrefix, StringComparison.Ordinal))
            return false;

        if (url.Contains('?') || url.Contains('#'))
            return false;

        var storageKey = url[LocalMediaPathPrefix.Length..];
        return UploadedImagePolicy.GetContentTypeForStorageKey(storageKey) != null
               && Path.GetFileName(storageKey) == storageKey;
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
