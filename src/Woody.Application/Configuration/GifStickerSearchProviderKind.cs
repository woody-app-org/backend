namespace Woody.Application.Configuration;

/// <summary>Provedor de pesquisa GIF/sticker para mensagens diretas (valor em <see cref="GifStickerSearchOptions.Provider"/>).</summary>
public enum GifStickerSearchProviderKind
{
    Local = 0,
    Klipy = 1,
    Giphy = 2,
}
