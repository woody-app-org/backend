namespace Woody.Domain.Entities;

/// <summary>
/// URLs (ou caminhos públicos) anexadas ao post. <see cref="StorageKey"/> reserva espaço para identificador no storage quando houver upload direto.
/// </summary>
public class PostImage
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string Url { get; set; } = null!;

    /// <summary>Ordem de exibição (0 = primeira).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Chave no blob/storage; preenchida quando o upload for implementado.</summary>
    public string? StorageKey { get; set; }
}
