using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Woody.Application.Utilities;

public static class CommunitySlugHelper
{
    public static string SlugifyBase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "comunidade";

        var normalized = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_')
                sb.Append('-');
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);
        slug = Regex.Replace(slug, "-+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "comunidade" : slug;
    }
}
