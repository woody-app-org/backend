using Woody.Application.DTOs;

namespace Woody.Application.Utilities;

public static class CreateCommunityRequestValidator
{
    private static readonly HashSet<string> AllowedCategories =
        new(StringComparer.OrdinalIgnoreCase) { "bemestar", "carreira", "cultura", "seguranca", "outro" };

    public static string? Validate(CreateCommunityRequestDTO dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;
        if (name.Length < 2)
            return "Informe um nome com pelo menos 2 caracteres.";
        if (name.Length > 80)
            return "Nome da comunidade muito longo.";

        var description = dto.Description?.Trim() ?? string.Empty;
        if (description.Length < 10)
            return "A descrição precisa de pelo menos 10 caracteres.";
        if (description.Length > 2_000)
            return "Descrição muito longa (máx. 2000 caracteres).";

        var category = dto.Category?.Trim() ?? string.Empty;
        if (!AllowedCategories.Contains(category))
            return "Categoria inválida.";

        var tags = NormalizeTags(dto.Tags);
        if (tags.Count > 12)
            return "Muitas tags (máx. 12).";
        if (tags.Any(t => t.Length > 40))
            return "Uma das tags é muito longa.";

        var rules = dto.Rules?.Trim() ?? string.Empty;
        if (rules.Length > 4_000)
            return "Regras muito longas (máx. 4000 caracteres).";

        var visibility = dto.Visibility?.Trim() ?? string.Empty;
        if (!string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase))
            return "Visibilidade inválida (use public ou private).";

        return null;
    }

    public static List<string> NormalizeTags(IEnumerable<string>? tags)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outList = new List<string>();
        if (tags == null)
            return outList;
        foreach (var t in tags)
        {
            var s = t.Trim();
            if (s.Length == 0) continue;
            if (!seen.Add(s)) continue;
            outList.Add(s);
        }

        return outList;
    }
}
