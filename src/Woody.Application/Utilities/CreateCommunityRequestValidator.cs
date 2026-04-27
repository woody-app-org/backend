using Woody.Application.DTOs;
using Woody.Application.Validation;

namespace Woody.Application.Utilities;

public static class CreateCommunityRequestValidator
{
    private static readonly HashSet<string> AllowedCategories =
        new(StringComparer.OrdinalIgnoreCase) { "bemestar", "carreira", "cultura", "seguranca", "outro" };

    public static string? Validate(CreateCommunityRequestDTO dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;
        if (name.Length < InputValidationLimits.CommunityNameMinLength)
            return "Informe um nome com pelo menos 2 caracteres.";
        if (name.Length > InputValidationLimits.CommunityNameMaxLength)
            return "Nome da comunidade muito longo.";

        var description = dto.Description?.Trim() ?? string.Empty;
        if (description.Length < InputValidationLimits.CommunityDescriptionMinLength)
            return "A descrição precisa de pelo menos 10 caracteres.";
        if (description.Length > InputValidationLimits.CommunityDescriptionMaxLength)
            return "Descrição muito longa (máx. 2000 caracteres).";

        var category = dto.Category?.Trim() ?? string.Empty;
        if (!AllowedCategories.Contains(category))
            return "Categoria inválida.";

        var tags = NormalizeTags(dto.Tags);
        if (tags.Count > InputValidationLimits.CommunityTagsMaxCount)
            return "Muitas tags (máx. 12).";
        if (tags.Any(t => t.Length > InputValidationLimits.CommunityTagMaxLength))
            return "Uma das tags é muito longa.";

        var rules = dto.Rules?.Trim() ?? string.Empty;
        if (rules.Length > InputValidationLimits.CommunityRulesMaxLength)
            return "Regras muito longas (máx. 4000 caracteres).";

        var visibility = dto.Visibility?.Trim() ?? string.Empty;
        if (!string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase))
            return "Visibilidade inválida (use public ou private).";

        if (!InputValidator.TryNormalizeHttpsImageUrl(dto.AvatarUrl, out _, out _)
            || !InputValidator.TryNormalizeHttpsImageUrl(dto.CoverUrl, out _, out _))
            return InputValidator.InvalidImageUrlMessage;

        return null;
    }

    public static string? ValidatePatch(CommunityUpdateRequestDTO dto)
    {
        if (dto.Name != null)
        {
            var name = dto.Name.Trim();
            if (name.Length < InputValidationLimits.CommunityNameMinLength)
                return "Informe um nome com pelo menos 2 caracteres.";
            if (name.Length > InputValidationLimits.CommunityNameMaxLength)
                return "Nome da comunidade muito longo.";
        }

        if (dto.Description != null)
        {
            var description = dto.Description.Trim();
            if (description.Length < InputValidationLimits.CommunityDescriptionMinLength)
                return "A descrição precisa de pelo menos 10 caracteres.";
            if (description.Length > InputValidationLimits.CommunityDescriptionMaxLength)
                return "Descrição muito longa (máx. 2000 caracteres).";
        }

        if (dto.Category != null)
        {
            var category = dto.Category.Trim();
            if (!AllowedCategories.Contains(category))
                return "Categoria inválida.";
        }

        if (dto.Tags != null)
        {
            var tags = NormalizeTags(dto.Tags);
            if (tags.Count > InputValidationLimits.CommunityTagsMaxCount)
                return "Muitas tags (máx. 12).";
            if (tags.Any(t => t.Length > InputValidationLimits.CommunityTagMaxLength))
                return "Uma das tags é muito longa.";
        }

        if (dto.Rules != null && dto.Rules.Trim().Length > InputValidationLimits.CommunityRulesMaxLength)
            return "Regras muito longas (máx. 4000 caracteres).";

        if (dto.Visibility != null)
        {
            var visibility = dto.Visibility.Trim();
            if (!string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase))
                return "Visibilidade inválida (use public ou private).";
        }

        if (!InputValidator.TryNormalizeHttpsImageUrl(dto.AvatarUrl, out _, out _)
            || !InputValidator.TryNormalizeHttpsImageUrl(dto.CoverUrl, out _, out _))
            return InputValidator.InvalidImageUrlMessage;

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
