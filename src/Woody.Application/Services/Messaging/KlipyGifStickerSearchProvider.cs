using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces.Messaging;

namespace Woody.Application.Services.Messaging;

/// <summary>
/// Espaço reservado para integração HTTP com Klipy.
/// Até à implementação real, pode delegar ao catálogo local conforme <see cref="GifStickerSearchOptions.EnableFallbackToLocal"/>.
/// </summary>
public sealed class KlipyGifStickerSearchProvider : IGifStickerSearchProvider
{
    public string ProviderKey => "klipy";

    private readonly LocalCatalogGifStickerSearchProvider _localCatalog;
    private readonly GifStickerSearchOptions _options;
    private readonly ILogger<KlipyGifStickerSearchProvider> _logger;

    public KlipyGifStickerSearchProvider(
        LocalCatalogGifStickerSearchProvider localCatalog,
        IOptions<GifStickerSearchOptions> options,
        ILogger<KlipyGifStickerSearchProvider> logger)
    {
        _localCatalog = localCatalog;
        _options = options.Value;
        _logger = logger;
    }

    public Task<GifStickerSearchResponseDto> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Integração Klipy (HTTP) será adicionada numa etapa seguinte.
        if (_options.EnableFallbackToLocal)
        {
            _logger.LogDebug(
                "Klipy: API real ainda não configurada; a usar catálogo local (EnableFallbackToLocal=true).");
            return SearchViaLocalFallbackAsync(query, limit, cancellationToken);
        }

        _logger.LogWarning(
            "Klipy: sem fallback local; devolvendo lista vazia até existir cliente Klipy.");

        return Task.FromResult(new GifStickerSearchResponseDto
        {
            Items = Array.Empty<GifStickerSearchItemDto>(),
            ProviderKey = ProviderKey,
        });
    }

    private async Task<GifStickerSearchResponseDto> SearchViaLocalFallbackAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken)
    {
        var inner = await _localCatalog.SearchAsync(query, limit, cancellationToken).ConfigureAwait(false);
        return new GifStickerSearchResponseDto
        {
            Items = inner.Items,
            ProviderKey = ProviderKey,
        };
    }
}
