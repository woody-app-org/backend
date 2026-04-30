using Woody.Domain.Media;

namespace Woody.Application.Validation;

public static class InputValidator
{
    public const string InvalidImageUrlMessage = "URL de imagem inválida. Use uma URL https válida.";

    public static bool TryNormalizeRequiredText(
        string? raw,
        string fieldName,
        int maxLength,
        out string normalized,
        out string? error,
        int minLength = 1)
    {
        normalized = string.Empty;
        error = null;

        var value = raw?.Trim() ?? string.Empty;
        if (value.Length < minLength)
        {
            error = $"{fieldName} é obrigatório.";
            return false;
        }

        if (value.Length > maxLength)
        {
            error = $"{fieldName} não pode exceder {maxLength} caracteres.";
            return false;
        }

        normalized = value;
        return true;
    }

    public static bool TryNormalizeOptionalText(
        string? raw,
        string fieldName,
        int maxLength,
        out string? normalized,
        out string? error)
    {
        normalized = null;
        error = null;

        if (raw == null)
            return true;

        var value = raw.Trim();
        if (value.Length == 0)
            return true;

        if (value.Length > maxLength)
        {
            error = $"{fieldName} não pode exceder {maxLength} caracteres.";
            return false;
        }

        normalized = value;
        return true;
    }

    public static bool TryNormalizeHttpsImageUrl(
        string? raw,
        out string? normalized,
        out string? error)
    {
        normalized = null;
        error = null;

        if (raw == null)
            return true;

        var value = raw.Trim();
        if (value.Length == 0)
            return true;

        if (!PublicImageUrlPolicy.IsPermittedImageUrl(value))
        {
            error = InvalidImageUrlMessage;
            return false;
        }

        normalized = value;
        return true;
    }

    public const string InvalidVideoUrlMessage = "URL de vídeo inválida. Use mp4/webm/mov via https ou um vídeo enviado à plataforma.";
    public const string InvalidGifUrlMessage = "URL de GIF inválida.";

    public static bool TryNormalizeHttpsVideoUrl(string? raw, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (raw == null)
            return true;

        var value = raw.Trim();
        if (value.Length == 0)
            return true;

        if (!PublicVideoUrlPolicy.IsPermittedVideoUrl(value))
        {
            error = InvalidVideoUrlMessage;
            return false;
        }

        normalized = value;
        return true;
    }

    /// <summary>GIF em posts: data <c>image/gif</c>, URL https .gif ou ficheiro local .gif.</summary>
    public static bool TryNormalizeHttpsGifUrl(string? raw, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (raw == null)
            return true;

        var value = raw.Trim();
        if (value.Length == 0)
            return true;

        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("data:image/gif", StringComparison.Ordinal))
        {
            if (lower.StartsWith("data:image/svg", StringComparison.Ordinal))
            {
                error = InvalidGifUrlMessage;
                return false;
            }

            if (!lower.Contains(";base64,", StringComparison.Ordinal))
            {
                error = InvalidGifUrlMessage;
                return false;
            }

            normalized = value;
            return true;
        }

        if (!PublicImageUrlPolicy.IsPermittedImageUrl(value))
        {
            error = InvalidGifUrlMessage;
            return false;
        }

        if (value.StartsWith(PublicImageUrlPolicy.LocalMediaPathPrefix, StringComparison.Ordinal))
        {
            if (!value.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                error = InvalidGifUrlMessage;
                return false;
            }

            normalized = value;
            return true;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            normalized = value;
            return true;
        }

        error = InvalidGifUrlMessage;
        return false;
    }

    /// <summary>Imagem ou sticker em posts: URL pública ou data URL raster (composer).</summary>
    public static bool TryNormalizePostImageOrDataUrl(string? raw, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (raw == null)
            return true;

        var value = raw.Trim();
        if (value.Length == 0)
            return true;

        if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var lower = value.ToLowerInvariant();
            if (lower.StartsWith("data:image/svg", StringComparison.Ordinal))
            {
                error = InvalidImageUrlMessage;
                return false;
            }

            if (!lower.Contains(";base64,", StringComparison.Ordinal))
            {
                error = InvalidImageUrlMessage;
                return false;
            }

            if (!lower.StartsWith("data:image/png", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/jpeg", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/jpg", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/webp", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/gif", StringComparison.Ordinal))
            {
                error = InvalidImageUrlMessage;
                return false;
            }

            normalized = value;
            return true;
        }

        return TryNormalizeHttpsImageUrl(raw, out normalized, out error);
    }

    public static bool TryNormalizeTags(
        IEnumerable<string>? raw,
        int maxCount,
        int maxLength,
        out List<string> normalized,
        out string? error)
    {
        normalized = new List<string>();
        error = null;

        if (raw == null)
            return true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            var value = item.Trim();
            if (value.Length > maxLength)
            {
                error = $"Uma das tags não pode exceder {maxLength} caracteres.";
                return false;
            }

            if (!seen.Add(value))
                continue;

            normalized.Add(value);
            if (normalized.Count > maxCount)
            {
                error = $"Máximo de {maxCount} tags.";
                return false;
            }
        }

        return true;
    }
}
