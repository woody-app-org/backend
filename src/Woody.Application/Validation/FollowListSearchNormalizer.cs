namespace Woody.Application.Validation;

/// <summary>
/// Normaliza o parâmetro <c>search</c> das listas paginadas de seguidores/seguindo.
/// </summary>
public static class FollowListSearchNormalizer
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('@'))
            trimmed = trimmed[1..].TrimStart();

        if (trimmed.Length == 0)
            return null;

        if (trimmed.Length > InputValidationLimits.FollowListSearchMaxLength)
            trimmed = trimmed[..InputValidationLimits.FollowListSearchMaxLength];

        return trimmed.ToLowerInvariant();
    }
}
