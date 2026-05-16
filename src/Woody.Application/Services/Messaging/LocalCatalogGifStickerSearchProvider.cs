using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces.Messaging;

namespace Woody.Application.Services.Messaging;

/// <summary>
/// Catálogo estático de GIF/stickers com URLs HTTPS públicas (Wikimedia Commons) — sem API keys, sem upload.
/// Catálogo estático usado quando a configuração <c>GifStickerSearch:Provider</c> é Local ou como fallback.
/// </summary>
public sealed class LocalCatalogGifStickerSearchProvider : IGifStickerSearchProvider
{
    public string ProviderKey => "local_catalog";

    private sealed record Entry(string ExternalId, string Title, string MediaType, string Url, string? ThumbnailUrl, string SearchBlob);

    private static readonly Entry[] Catalog =
    {
        new(
            "wm-earth",
            "Planeta Terra",
            "gif",
            "https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif",
            null,
            "terra planeta espaço gif earth"),
        new(
            "wm-hourglass",
            "Ampulheta",
            "gif",
            "https://upload.wikimedia.org/wikipedia/commons/4/4d/Hourglass_3.gif",
            null,
            "ampulheta tempo hourglass gif"),
        new(
            "wm-loading",
            "A carregar",
            "gif",
            "https://upload.wikimedia.org/wikipedia/commons/b/b1/Loading_icon.gif",
            null,
            "loading carregar espera gif"),
        new(
            "wm-png-demo",
            "Sticker PNG (transparência)",
            "sticker",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/PNG_transparency_demonstration_1.png/200px-PNG_transparency_demonstration_1.png",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/PNG_transparency_demonstration_1.png/80px-PNG_transparency_demonstration_1.png",
            "sticker png transparente demo"),
        new(
            "wm-smiley",
            "Smiley",
            "sticker",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/8/85/Smiley.svg/200px-Smiley.svg.png",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/8/85/Smiley.svg/80px-Smiley.svg.png",
            "smiley feliz sticker png"),
        new(
            "wm-wiki-favicon",
            "Wikipedia (ícone)",
            "sticker",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Wikipedia_favicon_2012.png/128px-Wikipedia_favicon_2012.png",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Wikipedia_favicon_2012.png/48px-Wikipedia_favicon_2012.png",
            "wikipedia favicon logo sticker"),
    };

    public Task<GifStickerSearchResponseDto> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var take = Math.Clamp(limit, 1, 40);
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<Entry> seq = Catalog;
        if (q.Length > 0)
        {
            var tokens = q.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            seq = Catalog.Where(e =>
            {
                var blob = (e.Title + " " + e.SearchBlob).ToLowerInvariant();
                return tokens.All(t => blob.Contains(t, StringComparison.Ordinal));
            });
        }

        var items = seq
            .Take(take)
            .Select(e => new GifStickerSearchItemDto
            {
                Title = e.Title,
                Url = e.Url,
                ThumbnailUrl = e.ThumbnailUrl,
                MediaType = e.MediaType,
                Provider = ProviderKey,
                ExternalId = e.ExternalId,
            })
            .ToList();

        return Task.FromResult(new GifStickerSearchResponseDto
        {
            Items = items,
            ProviderKey = ProviderKey,
        });
    }
}
