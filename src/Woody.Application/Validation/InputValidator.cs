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
