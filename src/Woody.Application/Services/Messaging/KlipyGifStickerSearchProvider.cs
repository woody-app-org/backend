using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces.Messaging;

namespace Woody.Application.Services.Messaging;

/// <summary>
/// Integração com a API Klipy (<c>api/v1/{app_key}/gifs|stickers/…</c>).
/// Combina resultados GIF + sticker; falhas upstream degradam para <see cref="LocalCatalogGifStickerSearchProvider"/> quando configurado.
/// </summary>
public sealed class KlipyGifStickerSearchProvider : IGifStickerSearchProvider
{
    public const string HttpClientName = "KlipyGifSticker";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<GifStickerSearchOptions> _gifStickerOptions;
    private readonly LocalCatalogGifStickerSearchProvider _localCatalog;
    private readonly ILogger<KlipyGifStickerSearchProvider> _logger;

    public KlipyGifStickerSearchProvider(
        IHttpClientFactory httpFactory,
        IOptions<GifStickerSearchOptions> gifStickerOptions,
        LocalCatalogGifStickerSearchProvider localCatalog,
        ILogger<KlipyGifStickerSearchProvider> logger)
    {
        _httpFactory = httpFactory;
        _gifStickerOptions = gifStickerOptions;
        _localCatalog = localCatalog;
        _logger = logger;
    }

    public string ProviderKey => "klipy";

    public async Task<GifStickerSearchResponseDto> SearchAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var opts = _gifStickerOptions.Value;
        var klipy = opts.Klipy;

        if (string.IsNullOrWhiteSpace(klipy.ApiKey))
        {
            _logger.LogWarning("Klipy: ApiKey não configurada.");
            return await TryFallbackAsync(opts, query, limit, cancellationToken).ConfigureAwait(false);
        }

        var trimmedQuery = query?.Trim();
        var gifSlice = (limit + 1) / 2;
        var stickerSlice = limit / 2;

        try
        {
            List<GifStickerSearchItemDto> gifItems;
            List<GifStickerSearchItemDto> stickerItems;

            if (string.IsNullOrEmpty(trimmedQuery))
            {
                gifItems = await FetchGifTrendingAsync(gifSlice, cancellationToken).ConfigureAwait(false);
                stickerItems = await FetchStickerTrendingAsync(stickerSlice, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                gifItems = await FetchGifSearchAsync(trimmedQuery, gifSlice, cancellationToken).ConfigureAwait(false);
                stickerItems = await FetchStickerSearchAsync(trimmedQuery, stickerSlice, cancellationToken)
                    .ConfigureAwait(false);
            }

            var merged = MergeInterleaved(gifItems, stickerItems, limit);

            if (merged.Count == 0)
            {
                _logger.LogWarning("Klipy: resposta vazia após filtrar URLs.");
                return await TryFallbackAsync(opts, query, limit, cancellationToken).ConfigureAwait(false);
            }

            return new GifStickerSearchResponseDto
            {
                Items = merged,
                ProviderKey = ProviderKey,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Klipy: falha inesperada na pesquisa.");
            return await TryFallbackAsync(opts, query, limit, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<GifStickerSearchResponseDto> TryFallbackAsync(
        GifStickerSearchOptions opts,
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!opts.EnableFallbackToLocal)
        {
            return new GifStickerSearchResponseDto
            {
                Items = Array.Empty<GifStickerSearchItemDto>(),
                ProviderKey = ProviderKey,
            };
        }

        var inner = await _localCatalog.SearchAsync(query, limit, cancellationToken).ConfigureAwait(false);
        return new GifStickerSearchResponseDto
        {
            Items = inner.Items,
            ProviderKey = ProviderKey,
        };
    }

    private async Task<List<GifStickerSearchItemDto>> FetchGifTrendingAsync(int take, CancellationToken ct)
    {
        if (take <= 0)
            return [];

        var perPage = NormalizeTrendingPerPage(take);
        var path = $"gifs/trending?page=1&per_page={perPage}";
        await using var doc = await GetKlipyJsonDocumentAsync(path, ct).ConfigureAwait(false);
        return doc == null ? [] : MapGifItems(ExtractItems(doc), take);
    }

    private async Task<List<GifStickerSearchItemDto>> FetchStickerTrendingAsync(int take, CancellationToken ct)
    {
        if (take <= 0)
            return [];

        var perPage = NormalizeTrendingPerPage(take);
        var path = $"stickers/trending?page=1&per_page={perPage}";
        await using var doc = await GetKlipyJsonDocumentAsync(path, ct).ConfigureAwait(false);
        return doc == null ? [] : MapStickerItems(ExtractItems(doc), take);
    }

    private async Task<List<GifStickerSearchItemDto>> FetchGifSearchAsync(string q, int take, CancellationToken ct)
    {
        if (take <= 0)
            return [];

        var perPage = NormalizeSearchPerPage(take);
        var path = $"gifs/search?page=1&per_page={perPage}&q={Uri.EscapeDataString(q)}";
        await using var doc = await GetKlipyJsonDocumentAsync(path, ct).ConfigureAwait(false);
        return doc == null ? [] : MapGifItems(ExtractItems(doc), take);
    }

    private async Task<List<GifStickerSearchItemDto>> FetchStickerSearchAsync(string q, int take, CancellationToken ct)
    {
        if (take <= 0)
            return [];

        var perPage = NormalizeSearchPerPage(take);
        var path = $"stickers/search?page=1&per_page={perPage}&q={Uri.EscapeDataString(q)}";
        await using var doc = await GetKlipyJsonDocumentAsync(path, ct).ConfigureAwait(false);
        return doc == null ? [] : MapStickerItems(ExtractItems(doc), take);
    }

    private async Task<JsonDocument?> GetKlipyJsonDocumentAsync(string pathAndQuery, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            var uri = BuildRequestUri(pathAndQuery);
            using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Klipy: HTTP {Status} para '{Endpoint}'.",
                    (int)resp.StatusCode,
                    pathAndQuery.Split('?', 2)[0]);
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            _logger.LogWarning(ex, "Klipy: pedido ou JSON inválido para '{Endpoint}'.", pathAndQuery.Split('?', 2)[0]);
            return null;
        }
    }

    private Uri BuildRequestUri(string pathAndQuery)
    {
        var klipy = _gifStickerOptions.Value.Klipy;
        var apiKey = klipy.ApiKey.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(klipy.BaseUrl) ? "https://api.klipy.com/" : klipy.BaseUrl.Trim();
        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";

        var keySegment = Uri.EscapeDataString(apiKey);
        var combined = $"{baseUrl}api/v1/{keySegment}/{pathAndQuery.TrimStart('/')}";
        return new Uri(combined, UriKind.Absolute);
    }

    private static int NormalizeTrendingPerPage(int requested) => Math.Clamp(requested, 1, 50);

    /// <summary>Klipy search API exige mínimo 8 itens por página.</summary>
    private static int NormalizeSearchPerPage(int requested) => Math.Clamp(Math.Max(requested, 8), 8, 50);

    private static List<JsonElement> ExtractItems(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.TryGetProperty("result", out var ok) && ok.ValueKind == JsonValueKind.False)
            return [];

        if (!root.TryGetProperty("data", out var dataEl))
            return [];

        if (dataEl.TryGetProperty("data", out var nested) && nested.ValueKind == JsonValueKind.Array)
            return nested.EnumerateArray().ToList();

        if (dataEl.ValueKind == JsonValueKind.Array)
            return dataEl.EnumerateArray().ToList();

        return [];
    }

    private List<GifStickerSearchItemDto> MapGifItems(IReadOnlyList<JsonElement> raw, int max)
    {
        var list = new List<GifStickerSearchItemDto>(Math.Min(max, raw.Count));
        foreach (var item in raw)
        {
            if (list.Count >= max)
                break;
            var dto = TryMapGif(item);
            if (dto != null)
                list.Add(dto);
        }

        return list;
    }

    private List<GifStickerSearchItemDto> MapStickerItems(IReadOnlyList<JsonElement> raw, int max)
    {
        var list = new List<GifStickerSearchItemDto>(Math.Min(max, raw.Count));
        foreach (var item in raw)
        {
            if (list.Count >= max)
                break;
            var dto = TryMapSticker(item);
            if (dto != null)
                list.Add(dto);
        }

        return list;
    }

    private GifStickerSearchItemDto? TryMapGif(JsonElement item)
    {
        if (!item.TryGetProperty("file", out var file))
            return null;

        var url = PickGifPlaybackUrl(file);
        if (!IsValidGifUrl(url))
            return null;

        var thumb = PickStillThumbnail(file);
        var title = ReadTitle(item, "GIF");
        var externalId = ReadExternalId(item);

        return new GifStickerSearchItemDto
        {
            Title = title,
            Url = url!,
            ThumbnailUrl = thumb,
            MediaType = "gif",
            Provider = "klipy",
            ExternalId = externalId,
        };
    }

    private GifStickerSearchItemDto? TryMapSticker(JsonElement item)
    {
        if (!item.TryGetProperty("file", out var file))
            return null;

        var url = PickStickerDisplayUrl(file, out var asGif);
        if (url == null || !IsHttpsAbsolute(url))
            return null;

        if (!asGif && !IsStickerRasterUrl(url))
            return null;

        var thumb = PickStillThumbnail(file);
        var title = ReadTitle(item, "Sticker");
        var externalId = ReadExternalId(item);
        var mediaType = asGif ? "gif" : "sticker";

        return new GifStickerSearchItemDto
        {
            Title = title,
            Url = url,
            ThumbnailUrl = thumb,
            MediaType = mediaType,
            Provider = "klipy",
            ExternalId = externalId,
        };
    }

    private static string ReadTitle(JsonElement item, string fallbackTitle)
    {
        if (item.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
        {
            var s = t.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s!;
        }

        return fallbackTitle;
    }

    private static string ReadExternalId(JsonElement item)
    {
        if (item.TryGetProperty("slug", out var slug) && slug.ValueKind == JsonValueKind.String)
        {
            var s = slug.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s!;
        }

        if (item.TryGetProperty("id", out var id))
            return id.ValueKind == JsonValueKind.String ? id.GetString() ?? "unknown" : id.GetRawText();

        return "unknown";
    }

    /// <summary>Ordem: hd → sd → xs; formato gif dentro do nível.</summary>
    private static string? PickGifPlaybackUrl(JsonElement file)
    {
        foreach (var tierName in new[] { "hd", "sd", "xs" })
        {
            if (!file.TryGetProperty(tierName, out var tier))
                continue;
            if (!tier.TryGetProperty("gif", out var gifEl))
                continue;
            if (gifEl.TryGetProperty("url", out var urlEl))
            {
                var s = urlEl.GetString();
                if (IsValidGifUrl(s))
                    return s;
            }
        }

        return null;
    }

    private static string? PickStickerDisplayUrl(JsonElement file, out bool isGif)
    {
        isGif = false;
        foreach (var tierName in new[] { "hd", "sd", "xs" })
        {
            if (!file.TryGetProperty(tierName, out var tier))
                continue;

            if (tier.TryGetProperty("webp", out var webp) && webp.TryGetProperty("url", out var wu))
            {
                var w = wu.GetString();
                if (IsHttpsAbsolute(w) && w!.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    return w;
            }

            if (tier.TryGetProperty("png", out var png) && png.TryGetProperty("url", out var pu))
            {
                var p = pu.GetString();
                if (IsHttpsAbsolute(p) && p!.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    return p;
            }
        }

        var gif = PickGifPlaybackUrl(file);
        if (gif != null)
        {
            isGif = true;
            return gif;
        }

        return null;
    }

    private static string? PickStillThumbnail(JsonElement file)
    {
        foreach (var tierName in new[] { "xs", "sd", "hd" })
        {
            if (!file.TryGetProperty(tierName, out var tier))
                continue;

            if (tier.TryGetProperty("jpg", out var jpg) && jpg.TryGetProperty("url", out var ju))
            {
                var j = ju.GetString();
                if (IsHttpsAbsolute(j))
                    return j;
            }

            if (tier.TryGetProperty("webp", out var webp) && webp.TryGetProperty("url", out var wu))
            {
                var w = wu.GetString();
                if (IsHttpsAbsolute(w))
                    return w;
            }
        }

        return null;
    }

    private static bool IsValidGifUrl(string? url) =>
        IsHttpsAbsolute(url) && url!.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

    private static bool IsHttpsAbsolute(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u)
               && string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStickerRasterUrl(string url) =>
        url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
        || url.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    private static List<GifStickerSearchItemDto> MergeInterleaved(
        IReadOnlyList<GifStickerSearchItemDto> gifs,
        IReadOnlyList<GifStickerSearchItemDto> stickers,
        int limit)
    {
        var merged = new List<GifStickerSearchItemDto>(limit);
        var i = 0;
        var j = 0;
        while (merged.Count < limit && (i < gifs.Count || j < stickers.Count))
        {
            if (i < gifs.Count)
            {
                merged.Add(gifs[i]);
                i++;
                if (merged.Count >= limit)
                    break;
            }

            if (j < stickers.Count)
            {
                merged.Add(stickers[j]);
                j++;
            }
        }

        return merged;
    }
}
