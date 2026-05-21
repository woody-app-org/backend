using System.Text.RegularExpressions;

namespace Woody.Application.Validation;

/// <summary>
/// Regras únicas de username para registo, edição de perfil e resolução por handle.
/// </summary>
public static partial class UsernameInputValidator
{
    public const string TooShortMessage = "Nome de utilizador deve ter pelo menos 3 caracteres.";
    public const string InvalidCharactersMessage =
        "Use apenas letras minúsculas, números, ponto (.) e sublinhado (_).";

    /// <summary>Trim + lowercase. Não valida formato.</summary>
    public static string Normalize(string? raw) =>
        raw?.Trim().ToLowerInvariant() ?? string.Empty;

    public static bool TryValidate(string? raw, out string normalized, out string? error)
    {
        normalized = Normalize(raw);
        error = null;

        if (normalized.Length < InputValidationLimits.UsernameMinLength)
        {
            error = TooShortMessage;
            return false;
        }

        if (normalized.Length > InputValidationLimits.UsernameMaxLength)
        {
            error = $"Nome de utilizador não pode exceder {InputValidationLimits.UsernameMaxLength} caracteres.";
            return false;
        }

        if (!ValidUsernamePattern().IsMatch(normalized))
        {
            error = InvalidCharactersMessage;
            return false;
        }

        return true;
    }

    [GeneratedRegex("^[a-z0-9_.]+$")]
    private static partial Regex ValidUsernamePattern();
}
