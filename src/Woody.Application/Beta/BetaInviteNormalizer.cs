namespace Woody.Application.Beta;

public static class BetaInviteNormalizer
{
    /// <summary>Normaliza o código do convite para comparação persistente (trim + maiúsculas).</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        return raw.Trim().ToUpperInvariant();
    }
}
