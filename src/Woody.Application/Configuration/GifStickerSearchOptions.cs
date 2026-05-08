namespace Woody.Application.Configuration;

/// <summary>
/// Configuração da pesquisa plugável de GIF/stickers (<c>GifStickerSearch</c> em appsettings).
/// </summary>
public sealed class GifStickerSearchOptions
{
    /// <summary>Nome do provider: <see cref="GifStickerSearchProviderKind"/> (<c>Local</c>, <c>Klipy</c>, <c>Giphy</c>).</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Limite máximo de itens devolvidos por pedido (o pedido HTTP pode pedir menos).</summary>
    public int Limit { get; set; } = 32;

    /// <summary>Timeout por pesquisa (segundos), aplicado ao token de cancelamento.</summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Quando o provider remoto (ex.: Klipy) não está implementado ou falha,
    /// permite degradar para <see cref="LocalCatalogGifStickerSearchProvider"/>.
    /// </summary>
    public bool EnableFallbackToLocal { get; set; } = true;

    /// <summary>
    /// Resolve <see cref="Provider"/> para um valor de enum; nomes inválidos tratam-se como <see cref="GifStickerSearchProviderKind.Local"/>.
    /// </summary>
    /// <param name="providerNameWasInvalid">Verdadeiro se a string não era vazia e não correspondia a um membro conhecido.</param>
    public GifStickerSearchProviderKind GetResolvedKind(out bool providerNameWasInvalid)
    {
        providerNameWasInvalid = false;
        var raw = Provider?.Trim();
        if (string.IsNullOrEmpty(raw))
            return GifStickerSearchProviderKind.Local;

        if (Enum.TryParse<GifStickerSearchProviderKind>(raw, ignoreCase: true, out var kind))
            return kind;

        providerNameWasInvalid = true;
        return GifStickerSearchProviderKind.Local;
    }
}
