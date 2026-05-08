using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces.Messaging;

namespace Woody.Application.Services.Messaging;

/// <summary>
/// Aplica <see cref="GifStickerSearchOptions.Limit"/> e <see cref="GifStickerSearchOptions.TimeoutSeconds"/>
/// ao pedido delegado ao provider interno.
/// </summary>
public sealed class GifStickerSearchConfigurableWrapper : IGifStickerSearchProvider
{
    private readonly IGifStickerSearchProvider _inner;
    private readonly IOptions<GifStickerSearchOptions> _options;
    private readonly ILogger<GifStickerSearchConfigurableWrapper> _logger;

    public GifStickerSearchConfigurableWrapper(
        IGifStickerSearchProvider inner,
        IOptions<GifStickerSearchOptions> options,
        ILogger<GifStickerSearchConfigurableWrapper> logger)
    {
        _inner = inner;
        _options = options;
        _logger = logger;
    }

    public string ProviderKey => _inner.ProviderKey;

    public async Task<GifStickerSearchResponseDto> SearchAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        var maxLimit = Math.Max(1, opts.Limit);
        var capped = Math.Clamp(limit, 1, maxLimit);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutSec = Math.Clamp(opts.TimeoutSeconds, 1, 120);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

        try
        {
            return await _inner.SearchAsync(query, capped, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce)
            when (!cancellationToken.IsCancellationRequested && cts.IsCancellationRequested)
        {
            _logger.LogWarning(oce, "Pesquisa GIF/sticker excedeu TimeoutSeconds ({Timeout}s).", timeoutSec);
            return new GifStickerSearchResponseDto
            {
                Items = Array.Empty<GifStickerSearchItemDto>(),
                ProviderKey = _inner.ProviderKey,
            };
        }
    }
}
