using System.Net;
using System.Text;

namespace Woody.Application.Posts;

/// <summary>Renderiza HTML mínimo com meta tags Open Graph / Twitter Card.</summary>
public static class PostShareHtmlRenderer
{
    private const int RedirectDelaySeconds = 2;

    public static string Render(PostSharePageModel model)
    {
        var title = Encode(model.Title);
        var description = Encode(model.Description);
        var image = Encode(model.ImageUrl);
        var shareUrl = Encode(model.SharePageUrl);
        var frontendUrl = Encode(model.FrontendPostUrl);

        var bodyTitle = model.IsUnavailable
            ? "Este conteúdo não está disponível."
            : Encode(model.Title);
        var bodyDescription = model.IsUnavailable
            ? "Conteúdo disponível apenas para quem tem acesso na Woody."
            : Encode(model.Description);

        var sb = new StringBuilder(2048);
        sb.Append("<!DOCTYPE html><html lang=\"pt\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append($"<title>{title}</title>");
        sb.Append($"<meta property=\"og:title\" content=\"{title}\">");
        sb.Append($"<meta property=\"og:description\" content=\"{description}\">");
        sb.Append($"<meta property=\"og:image\" content=\"{image}\">");
        sb.Append($"<meta property=\"og:url\" content=\"{shareUrl}\">");
        sb.Append("<meta property=\"og:type\" content=\"article\">");
        sb.Append("<meta property=\"og:site_name\" content=\"Woody\">");
        sb.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");
        sb.Append($"<meta name=\"twitter:title\" content=\"{title}\">");
        sb.Append($"<meta name=\"twitter:description\" content=\"{description}\">");
        sb.Append($"<meta name=\"twitter:image\" content=\"{image}\">");
        sb.Append($"<link rel=\"canonical\" href=\"{shareUrl}\">");
        sb.Append($"<meta http-equiv=\"refresh\" content=\"{RedirectDelaySeconds};url={frontendUrl}\">");
        sb.Append("<style>");
        sb.Append("body{font-family:system-ui,-apple-system,Segoe UI,sans-serif;margin:0;padding:2rem;background:#faf8f5;color:#1a1410;}");
        sb.Append(".card{max-width:32rem;margin:0 auto;padding:1.5rem;border:1px solid #e8e0d8;border-radius:1rem;background:#fff;}");
        sb.Append("h1{font-size:1.25rem;margin:0 0 .75rem;}p{margin:0 0 1rem;line-height:1.5;color:#5c534a;}");
        sb.Append("a{display:inline-block;padding:.625rem 1rem;border-radius:.75rem;background:#2d5016;color:#fff;text-decoration:none;font-weight:600;}");
        sb.Append("a:hover{background:#243f12;}</style></head><body><main class=\"card\">");
        sb.Append($"<h1>{bodyTitle}</h1>");
        sb.Append($"<p>{bodyDescription}</p>");
        sb.Append($"<a href=\"{frontendUrl}\">Abrir na Woody</a>");
        sb.Append("</main>");
        sb.Append($"<script>setTimeout(function(){{window.location.href={JsString(model.FrontendPostUrl)};}},{RedirectDelaySeconds * 1000});</script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    internal static string Encode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

    internal static string JsString(string value)
    {
        var encoded = (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return $"'{encoded}'";
    }
}
