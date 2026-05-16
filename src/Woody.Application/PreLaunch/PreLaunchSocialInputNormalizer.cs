using System.Text.RegularExpressions;

namespace Woody.Application.PreLaunch;

public readonly record struct PreLaunchUsernameNormalizeResult(
    string DisplayUsername,
    string NormalizedUsername,
    bool Success,
    string? Error);

/// <summary>
/// Normaliza rede social e username para deduplicação e exibição, incluindo extração de URLs comuns.
/// </summary>
public static class PreLaunchSocialInputNormalizer
{
    private static readonly HashSet<string> AllowedNetworks = new(StringComparer.OrdinalIgnoreCase)
    {
        "instagram", "tiktok", "x", "threads", "facebook", "linkedin", "other"
    };

    private static readonly HashSet<string> FacebookReservedPath = new(StringComparer.OrdinalIgnoreCase)
    {
        "pages", "groups", "events", "watch", "marketplace", "gaming", "share", "dialog",
        "permalink.php", "story.php", "photo.php", "videos", "reel", "reels", "pg", "people",
        "login", "recover", "stories", "plugins", "help", "privacy", "legal", "policies",
        "settings", "messages", "notifications", "home", "me"
    };

    private static readonly HashSet<string> InstagramReservedPath = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "reel", "reels", "stories", "tv", "explore", "accounts", "share", "direct", "s"
    };

    private static readonly HashSet<string> TwitterReservedPath = new(StringComparer.OrdinalIgnoreCase)
    {
        "i", "intent", "search", "share", "home", "settings", "hashtags", "compose", "messages", "notifications"
    };

    public static string? TryNormalizeNetwork(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var t = PreLaunchTextSanitizer.RemoveControlCharacters(raw).Trim().ToLowerInvariant();
        return AllowedNetworks.Contains(t) ? t : null;
    }

    public static string SanitizeName(string? raw, int maxLen = 120)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = PreLaunchTextSanitizer.RemoveControlCharacters(raw).Trim();
        if (s.Length > maxLen)
            s = s[..maxLen];
        return s;
    }

    public static PreLaunchUsernameNormalizeResult NormalizeUsername(string normalizedNetwork, string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return new PreLaunchUsernameNormalizeResult(string.Empty, string.Empty, false, "Usuário inválido.");

        var cleaned = PreLaunchTextSanitizer.RemoveControlCharacters(rawInput).Trim();
        if (cleaned.Length == 0)
            return new PreLaunchUsernameNormalizeResult(string.Empty, string.Empty, false, "Usuário inválido.");

        var extracted = TryExtractHandleFromUrl(normalizedNetwork, cleaned) ?? cleaned;
        extracted = extracted.Trim();

        if (normalizedNetwork == "other")
            return NormalizeOtherUsername(extracted);

        extracted = extracted.TrimStart('@');
        extracted = Regex.Replace(extracted, @"\s+", "");

        if (extracted.Length == 0)
            return new PreLaunchUsernameNormalizeResult(string.Empty, string.Empty, false, "Usuário inválido.");

        if (extracted.Length > 80)
            return new PreLaunchUsernameNormalizeResult(string.Empty, string.Empty, false, "Usuário inválido.");

        var normalized = extracted.ToLowerInvariant();
        return new PreLaunchUsernameNormalizeResult(extracted, normalized, true, null);
    }

    private static PreLaunchUsernameNormalizeResult NormalizeOtherUsername(string extracted)
    {
        extracted = Regex.Replace(extracted, @"\s+", " ").Trim();
        if (extracted.Length > 80)
            extracted = extracted[..80];

        if (string.IsNullOrWhiteSpace(extracted))
            return new PreLaunchUsernameNormalizeResult(string.Empty, string.Empty, false, "Usuário inválido.");

        var normalized = extracted.ToLowerInvariant();
        return new PreLaunchUsernameNormalizeResult(extracted, normalized, true, null);
    }

    private static string? TryExtractHandleFromUrl(string network, string input)
    {
        var s = input.Trim();
        if (s.Length < 4)
            return null;

        var lower = s.ToLowerInvariant();
        if (!lower.Contains("://", StringComparison.Ordinal)
            && !lower.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            && !lower.Contains(".com/", StringComparison.OrdinalIgnoreCase)
            && !lower.Contains(".net/", StringComparison.OrdinalIgnoreCase))
            return null;

        return network switch
        {
            "instagram" => ExtractInstagramHandle(s),
            "tiktok" => ExtractTikTokHandle(s),
            "x" => ExtractTwitterHandle(s),
            "threads" => ExtractThreadsHandle(s),
            "facebook" => ExtractFacebookHandle(s),
            "linkedin" => ExtractLinkedInSlug(s),
            _ => null
        };
    }

    private static string? ExtractInstagramHandle(string s)
    {
        if (!ContainsHost(s, "instagram.com") && !ContainsHost(s, "instagr.am"))
            return null;

        var path = GetPathAndQuery(s);
        var lowerPath = path.ToLowerInvariant();
        if (lowerPath.StartsWith("/p/", StringComparison.Ordinal)
            || lowerPath.StartsWith("/reel/", StringComparison.Ordinal)
            || lowerPath.StartsWith("/reels/", StringComparison.Ordinal)
            || lowerPath.StartsWith("/stories/", StringComparison.Ordinal)
            || lowerPath.StartsWith("/tv/", StringComparison.Ordinal))
            return null;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var seg in segments)
        {
            if (seg.Contains('?', StringComparison.Ordinal))
                break;

            var part = seg.Split('?')[0];
            if (part.Length == 0 || InstagramReservedPath.Contains(part))
                continue;

            return Uri.UnescapeDataString(part);
        }

        return null;
    }

    private static string? ExtractTikTokHandle(string s)
    {
        if (!ContainsHost(s, "tiktok.com"))
            return null;

        var path = GetPathAndQuery(s);
        var m = Regex.Match(path, @"@([^/?#]+)", RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var seg in segments)
        {
            if (seg.Equals("video", StringComparison.OrdinalIgnoreCase)
                || seg.Equals("user", StringComparison.OrdinalIgnoreCase)
                || seg.Equals("share", StringComparison.OrdinalIgnoreCase))
                continue;

            if (seg.StartsWith('@'))
                return seg[1..];

            if (seg.Length > 0 && !ulong.TryParse(seg, out _))
                return seg;
        }

        return null;
    }

    private static string? ExtractTwitterHandle(string s)
    {
        if (!ContainsHost(s, "twitter.com") && !ContainsHost(s, "x.com"))
            return null;

        var path = GetPathAndQuery(s);
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var seg in segments)
        {
            var part = seg.Split('?')[0];
            if (part.Length == 0 || TwitterReservedPath.Contains(part))
                continue;

            if (part.Equals("status", StringComparison.OrdinalIgnoreCase)
                || part.Equals("i", StringComparison.OrdinalIgnoreCase))
                break;

            if (ulong.TryParse(part, out _))
                continue;

            return part.StartsWith('@') ? part[1..] : part;
        }

        return null;
    }

    private static string? ExtractThreadsHandle(string s)
    {
        if (!ContainsHost(s, "threads.net"))
            return null;

        var path = GetPathAndQuery(s);
        var m = Regex.Match(path, @"@([^/?#]+)", RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length > 0 ? segments[0].TrimStart('@') : null;
    }

    private static string? ExtractFacebookHandle(string s)
    {
        if (!ContainsHost(s, "facebook.com") && !ContainsHost(s, "fb.com"))
            return null;

        var path = GetPathAndQuery(s);
        if (path.Contains("profile.php", StringComparison.OrdinalIgnoreCase))
            return null;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var seg in segments)
        {
            var part = seg.Split('?')[0];
            if (part.Length == 0 || FacebookReservedPath.Contains(part))
                continue;

            if (ulong.TryParse(part, out _))
                continue;

            return Uri.UnescapeDataString(part.StartsWith('@') ? part[1..] : part);
        }

        return null;
    }

    private static string? ExtractLinkedInSlug(string s)
    {
        if (!ContainsHost(s, "linkedin.com"))
            return null;

        var path = GetPathAndQuery(s);
        var idx = path.IndexOf("/in/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var rest = path[(idx + 4)..];
        var slug = rest.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.IsNullOrEmpty(slug))
            return null;

        return slug.Split('?')[0];
    }

    private static bool ContainsHost(string input, string hostFragment)
    {
        var lower = input.ToLowerInvariant();
        return lower.Contains(hostFragment, StringComparison.Ordinal);
    }

    private static string GetPathAndQuery(string s)
    {
        var withScheme = s.StartsWith("//", StringComparison.Ordinal)
            ? "https:" + s
            : s.Contains("://", StringComparison.Ordinal)
                ? s
                : "https://" + s;

        if (Uri.TryCreate(withScheme, UriKind.Absolute, out var uri))
            return uri.PathAndQuery;

        var idx = s.IndexOf("://", StringComparison.Ordinal);
        if (idx >= 0)
        {
            var afterScheme = s[(idx + 3)..];
            var slash = afterScheme.IndexOf('/');
            if (slash < 0)
                return "/";

            return afterScheme[slash..];
        }

        // host/caminho sem esquema (ex.: instagram.com/maria)
        var firstSlash = s.IndexOf('/');
        if (firstSlash < 0)
            return "/";

        var hostPart = s[..firstSlash];
        if (hostPart.Contains('.', StringComparison.Ordinal))
            return s[firstSlash..];

        return s.StartsWith('/') ? s : "/" + s;
    }
}
