using System.Text.RegularExpressions;
using Woody.Domain.Media;

namespace Woody.Application.Validation;

/// <summary>Validação de GIF opcional em comentários: só https público, sem <c>data:</c>/<c>blob:</c>, alinhado ao picker de messaging.</summary>
public static class CommentGifAttachmentValidator
{
    /// <summary>Provedores aceites (alinhado a <c>GifStickerSearchItemDto.Provider</c> / picker de messaging).</summary>
    private static readonly HashSet<string> KnownProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "local_catalog",
        "klipy",
        "giphy",
        "tenor",
    };

    private static readonly Regex ExternalIdSyntax = new(
        @"^[a-zA-Z0-9_-]{1,128}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool HasAnyGifField(
        string? gifUrl,
        string? gifThumbnailUrl,
        string? gifProvider,
        string? gifExternalId,
        string? gifTitle)
    {
        static bool NonEmpty(string? s) => !string.IsNullOrWhiteSpace(s);

        return NonEmpty(gifUrl)
               || NonEmpty(gifThumbnailUrl)
               || NonEmpty(gifProvider)
               || NonEmpty(gifExternalId)
               || NonEmpty(gifTitle);
    }

    /// <summary>
    /// Valida conjunto GIF completo. Retorna <c>true</c> com <paramref name="normalized"/> preenchido ou
    /// <c>false</c> com <paramref name="error"/>; nunca devolve GIF parcial.
    /// </summary>
    public static bool TryNormalizeGifFields(
        string? gifUrl,
        string? gifThumbnailUrl,
        string? gifProvider,
        string? gifExternalId,
        string? gifTitle,
        out string normalizedUrl,
        out string? normalizedThumbnailUrl,
        out string normalizedProvider,
        out string normalizedExternalId,
        out string? normalizedTitle,
        out string? error)
    {
        normalizedUrl = string.Empty;
        normalizedThumbnailUrl = null;
        normalizedProvider = string.Empty;
        normalizedExternalId = string.Empty;
        normalizedTitle = null;
        error = null;

        if (string.IsNullOrWhiteSpace(gifUrl)
            || string.IsNullOrWhiteSpace(gifProvider)
            || string.IsNullOrWhiteSpace(gifExternalId))
        {
            error = "GIF incompleto: url, provedor e identificador externos são obrigatórios.";
            return false;
        }

        var pTrim = gifProvider.Trim();
        if (pTrim.Length > InputValidationLimits.CommentGifProviderMaxLength)
        {
            error = "Provedor do GIF inválido.";
            return false;
        }

        if (!KnownProviders.Contains(pTrim))
        {
            error = "Provedor do GIF inválido.";
            return false;
        }

        var idTrim = gifExternalId.Trim();
        if (idTrim.Length > InputValidationLimits.CommentGifExternalIdMaxLength
            || !ExternalIdSyntax.IsMatch(idTrim))
        {
            error = "Identificador externo do GIF inválido.";
            return false;
        }

        if (!TryNormalizeStrictExternalHttpsGifUrl(gifUrl, out var urlNorm, out var urlErr))
        {
            error = urlErr;
            return false;
        }

        if (!InputValidator.TryNormalizeHttpsImageUrl(gifThumbnailUrl, out var thumbNorm, out var thumbErr))
        {
            error = thumbErr;
            return false;
        }

        if (!InputValidator.TryNormalizeOptionalText(
                gifTitle,
                "Título do GIF",
                InputValidationLimits.CommentGifTitleMaxLength,
                out var titleNorm,
                out var titleErr))
        {
            error = titleErr;
            return false;
        }

        if (titleNorm != null && ContainsHtmlLikeMarkers(titleNorm))
        {
            error = "Título do GIF não pode conter HTML.";
            return false;
        }

        normalizedUrl = urlNorm!;
        normalizedThumbnailUrl = thumbNorm;
        normalizedProvider = pTrim;
        normalizedExternalId = idTrim;
        normalizedTitle = titleNorm;
        return true;
    }

    private static bool ContainsHtmlLikeMarkers(string s) => s.Contains('<') || s.Contains('>');

    /// <summary>HTTPS absoluto, domínio público, caminho termina em <c>.gif</c>; rejeita <c>data:</c>, <c>blob:</c>, <c>file:</c>, media local.</summary>
    public static bool TryNormalizeStrictExternalHttpsGifUrl(string? raw, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (raw == null)
        {
            error = InputValidator.InvalidGifUrlMessage;
            return false;
        }

        var value = raw.Trim();
        if (value.Length == 0)
        {
            error = InputValidator.InvalidGifUrlMessage;
            return false;
        }

        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("javascript:", StringComparison.Ordinal)
            || lower.StartsWith("data:", StringComparison.Ordinal)
            || lower.StartsWith("blob:", StringComparison.Ordinal)
            || lower.StartsWith("file:", StringComparison.Ordinal))
        {
            error = InputValidator.InvalidGifUrlMessage;
            return false;
        }

        if (!PublicImageUrlPolicy.IsPermittedExternalImageUrl(value))
        {
            error = InputValidator.InvalidGifUrlMessage;
            return false;
        }

        // Verificar extensão .gif no caminho (AbsolutePath) para não rejeitar URLs com query string
        // válidas como https://media.tenor.com/abc.gif?size=small
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            error = InputValidator.InvalidGifUrlMessage;
            return false;
        }

        normalized = value;
        return true;
    }
}
