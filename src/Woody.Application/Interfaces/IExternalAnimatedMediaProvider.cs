namespace Woody.Application.Interfaces;

/// <summary>
/// Provedor externo opcional para GIF/stickers (Giphy, Tenor, …). Registar implementação no DI quando existir chave API.
/// </summary>
public interface IExternalAnimatedMediaProvider
{
    Task<IReadOnlyList<ExternalAnimatedMediaItemDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

public sealed record ExternalAnimatedMediaItemDto(
    string PreviewUrl,
    string FullUrl,
    string Title,
    string Provider);
