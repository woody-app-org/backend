using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces.Messaging;

/// <summary>
/// Provedor plugável de pesquisa de GIF/sticker para o picker de conversas.
/// Implementações podem ser catálogo local, proxy a GIPHY/Tenor, etc.; a UI/API não acopla a um fornecedor.
/// </summary>
public interface IGifStickerSearchProvider
{
    /// <summary>Identificador estável (ex.: <c>local_catalog</c>) devolvido ao cliente para telemetria/depuração.</summary>
    string ProviderKey { get; }

    Task<GifStickerSearchResponseDto> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default);
}
