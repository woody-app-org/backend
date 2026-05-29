namespace Woody.Application.Utilities;

public static class EmailMasking
{
    /// <summary>
    /// Mascara e-mail para exibição (ex.: ni*****@gmail.com), derivado do input normalizado.
    /// </summary>
    public static string MaskForDisplay(string normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return "***@***";

        var at = normalizedEmail.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at == normalizedEmail.Length - 1)
            return "***@***";

        var local = normalizedEmail[..at];
        var domain = normalizedEmail[(at + 1)..];
        if (local.Length == 0)
            return $"***@{domain}";

        if (local.Length == 1)
            return $"{local[0]}***@{domain}";

        return $"{local[..2]}*****@{domain}";
    }
}
