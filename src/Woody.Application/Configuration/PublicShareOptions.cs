namespace Woody.Application.Configuration;

/// <summary>
/// Configuração de páginas HTML de partilha externa (Open Graph) para crawlers.
/// </summary>
public sealed class PublicShareOptions
{
    public const string SectionName = "PublicShare";

    /// <summary>Origem pública do frontend SPA (redirect humano e imagem fallback opcional).</summary>
    public string FrontendPublicOrigin { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Base absoluta das URLs de share (<c>/share/posts/…</c>).
    /// Se vazio, usa a origem do pedido HTTP (recomendado atrás de proxy com X-Forwarded-*).
    /// </summary>
    public string PublicShareBaseUrl { get; set; } = "";

    /// <summary>
    /// Origem pública da API para resolver URLs relativas de mídia (<c>/api/media/…</c>).
    /// Se vazio, usa a origem do pedido HTTP.
    /// </summary>
    public string ApiPublicOrigin { get; set; } = "";

    /// <summary>Imagem OG fallback (absoluta). Se vazio, usa <c>{FrontendPublicOrigin}/icon-512.png</c>.</summary>
    public string OgFallbackImageUrl { get; set; } = "";
}
