namespace Woody.Application.Validation;

/// <summary>
/// Regras de senha: sem espaços no cadastro; no login remove espaços acidentais antes da verificação.
/// </summary>
public static class PasswordInputValidator
{
    public const string ContainsWhitespaceMessage = "A senha não pode conter espaços.";

    /// <summary>
    /// Remove todo whitespace (espaços, tabs, quebras de linha) para comparação no login.
    /// </summary>
    public static string NormalizeForLogin(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        return RemoveAllWhitespace(raw);
    }

    public static bool TryValidateForRegistration(
        string? raw,
        int maxLength,
        int minLength,
        out string normalized,
        out string? error)
    {
        normalized = string.Empty;
        error = null;

        if (string.IsNullOrEmpty(raw))
        {
            error = "Senha é obrigatória.";
            return false;
        }

        if (ContainsAnyWhitespace(raw))
        {
            error = ContainsWhitespaceMessage;
            return false;
        }

        if (raw.Length < minLength)
        {
            error = $"Senha deve ter no mínimo {minLength} caracteres.";
            return false;
        }

        if (raw.Length > maxLength)
        {
            error = $"Senha não pode exceder {maxLength} caracteres.";
            return false;
        }

        normalized = raw;
        return true;
    }

    private static bool ContainsAnyWhitespace(string value)
    {
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c))
                return true;
        }

        return false;
    }

    private static string RemoveAllWhitespace(string value)
    {
        var write = 0;
        var chars = new char[value.Length];
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c))
                continue;
            chars[write++] = c;
        }

        return write == 0 ? string.Empty : new string(chars, 0, write);
    }
}
