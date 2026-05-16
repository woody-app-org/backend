namespace Woody.Application.Configuration;

/// <summary>Definições da integração HTTP Klipy (<c>GifStickerSearch:Klipy</c>).</summary>
public sealed class GifStickerSearchKlipyOptions
{
    /// <summary>Base da API (ex.: <c>https://api.klipy.com/</c>).</summary>
    public string BaseUrl { get; set; } = "https://api.klipy.com/";

    /// <summary>Chave de aplicação Klipy (apenas servidor; nunca expor ao cliente web).</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Timeout do <see cref="System.Net.Http.HttpClient"/> para pedidos Klipy (segundos).</summary>
    public int TimeoutSeconds { get; set; } = 5;
}

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

    /// <summary>Opções específicas quando <see cref="Provider"/> é Klipy.</summary>
    public GifStickerSearchKlipyOptions Klipy { get; set; } = new();

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
