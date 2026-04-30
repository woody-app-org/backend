using Woody.Domain.Entities.Enum;

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

    /// <summary>Discriminador semântico (imagem, vídeo curto, GIF animado, sticker).</summary>
    public MediaKind MediaKind { get; set; } = MediaKind.Image;

    /// <summary>MIME declarado ou inferido (opcional).</summary>
    public string? MimeType { get; set; }

    /// <summary>Duração em segundos para vídeo (opcional; validação exacta futura).</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Ordem de exibição (0 = primeira).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Chave no blob/storage; preenchida quando o upload for implementado.</summary>
    public string? StorageKey { get; set; }
}
