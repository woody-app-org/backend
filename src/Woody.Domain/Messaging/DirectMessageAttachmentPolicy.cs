using System.Net;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;
namespace Woody.Domain.Messaging;

/// <summary>
/// Regras de segurança para URLs de anexos em DMs (fonte de verdade no domínio).
/// Evita esquemas perigosos e data URLs não-imagem; alinhado ao envio de imagens no cliente (https ou data:image/*;base64).
/// </summary>
public static class DirectMessageAttachmentPolicy
{
    /// <summary>Indica se a URL pode ser persistida como anexo de imagem.</summary>
    public static bool IsPermittedAttachmentUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var t = url.Trim();
        if (t.Length < 12)
            return false;

        if (t.Contains('\r') || t.Contains('\n') || t.Contains('\0'))
            return false;

        if (PublicImageUrlPolicy.IsPermittedImageUrl(t))
            return true;

        if (!t.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return false;

        var lower = t.ToLowerInvariant();
        if (lower.StartsWith("data:image/svg", StringComparison.Ordinal))
            return false;

        if (!lower.Contains(";base64,", StringComparison.Ordinal))
            return false;

        return lower.StartsWith("data:image/png", StringComparison.Ordinal)
               || lower.StartsWith("data:image/jpeg", StringComparison.Ordinal)
               || lower.StartsWith("data:image/jpg", StringComparison.Ordinal)
               || lower.StartsWith("data:image/gif", StringComparison.Ordinal)
               || lower.StartsWith("data:image/webp", StringComparison.Ordinal);
    }

    /// <summary>Validação por tipo semântico (vídeo nunca reutiliza regras só de imagem).</summary>
    public static bool IsPermittedTypedAttachmentUrl(MediaKind kind, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var t = url.Trim();
        if (t.Contains('\r') || t.Contains('\n') || t.Contains('\0'))
            return false;

        return kind switch
        {
            MediaKind.Video => PublicVideoUrlPolicy.IsPermittedVideoUrl(t),
            MediaKind.Gif => IsPermittedGifUrl(t),
            MediaKind.Sticker => IsPermittedStickerUrl(t),
            _ => IsPermittedAttachmentUrl(t)
        };
    }

    private static bool IsPermittedGifUrl(string t)
    {
        var lower = t.ToLowerInvariant();
        if (lower.StartsWith("data:image/gif", StringComparison.Ordinal) && lower.Contains(";base64,", StringComparison.Ordinal))
            return !lower.StartsWith("data:image/svg", StringComparison.Ordinal);

        if (t.StartsWith(PublicImageUrlPolicy.LocalMediaPathPrefix, StringComparison.Ordinal))
        {
            var key = t[PublicImageUrlPolicy.LocalMediaPathPrefix.Length..];
            return key.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                   && MediaStorageKeySyntax.IsPermittedImageStorageKeyForLocalApi(key);
        }

        if (Uri.TryCreate(t, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            return IsSafePublicHttpsHost(uri);

        return false;
    }

    private static bool IsPermittedStickerUrl(string t)
    {
        if (IsPermittedAttachmentUrl(t))
            return true;

        if (Uri.TryCreate(t, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            var p = uri.AbsolutePath.ToLowerInvariant();
            if (p.EndsWith(".webp", StringComparison.Ordinal) || p.EndsWith(".png", StringComparison.Ordinal))
                return IsSafePublicHttpsHost(uri);
        }

        return false;
    }

    /// <summary>Mesmas restrições de host que <see cref="PublicVideoUrlPolicy"/> para URLs HTTPS.</summary>
    private static bool IsSafePublicHttpsHost(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(uri.Host))
            return false;
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!IPAddress.TryParse(uri.Host, out var address))
            return true;
        if (IPAddress.IsLoopback(address))
            return false;
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254))
                return false;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return !address.IsIPv6LinkLocal
                   && !address.IsIPv6SiteLocal
                   && !address.Equals(IPAddress.IPv6Loopback);
        }

        return true;
    }
}
