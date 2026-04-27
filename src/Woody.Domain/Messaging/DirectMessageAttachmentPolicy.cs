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

        if (t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return PublicImageUrlPolicy.IsPermittedExternalImageUrl(t);

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
}
