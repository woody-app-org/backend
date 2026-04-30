namespace Woody.Application.DTOs.Api;

/// <summary>Item devolvido pelo provedor de pesquisa GIF/sticker (independente do fornecedor real).</summary>
public sealed class GifStickerSearchItemDto
{
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    /// <summary><c>gif</c> ou <c>sticker</c></summary>
    public string MediaType { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
}

public sealed class GifStickerSearchResponseDto
{
    public IReadOnlyList<GifStickerSearchItemDto> Items { get; set; } = Array.Empty<GifStickerSearchItemDto>();
    /// <summary>Chave do provedor que serviu estes resultados.</summary>
    public string ProviderKey { get; set; } = null!;
}
