using System.Text.Json;

namespace Woody.Api.RateLimiting;

public static class AuthEmailRateLimitBodyParser
{
    /// <summary>
    /// Extrai e normaliza o campo "email" de um corpo JSON (POST Auth de verificação).
    /// </summary>
    public static string? TryExtractNormalizedEmail(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("email", out var el))
                return null;

            var raw = el.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return raw.Trim().ToLowerInvariant();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IsPlausibleRateLimitEmail(string email)
    {
        if (email.Length is < 3 or > 254)
            return false;

        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1)
            return false;

        return true;
    }

    /// <summary>
    /// Máscara para logs (ex.: b***@example.com).
    /// </summary>
    public static string MaskEmailForLog(string normalizedEmail)
    {
        var at = normalizedEmail.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0)
            return "***";

        var local = normalizedEmail[..at];
        var domain = normalizedEmail[(at + 1)..];
        if (local.Length == 0)
            return $"***@{domain}";

        var first = local[0];
        return $"{first}***@{domain}";
    }

    public static int StableHashForLog(string normalizedEmail)
    {
        unchecked
        {
            return normalizedEmail.GetHashCode(StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Extrai resetToken do corpo JSON (POST password-reset/confirm).
    /// </summary>
    public static string? TryExtractResetToken(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("resetToken", out var el))
                return null;

            var raw = el.GetString();
            return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Chave estável para rate limit de confirm sem armazenar token em claro.</summary>
    public static string? BuildResetTokenRateLimitKey(string resetToken)
    {
        if (string.IsNullOrWhiteSpace(resetToken))
            return null;

        var bytes = System.Text.Encoding.UTF8.GetBytes(resetToken);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
