using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Anexo multimédia partilhado entre posts e mensagens (uma tabela, sem modelagens paralelas).
/// <see cref="PostId"/> ou <see cref="MessageId"/> preenchido conforme <see cref="OwnerType"/> (FK para cascade).
/// </summary>
public class MediaAttachment
{
    public int Id { get; set; }

    public MediaOwnerType OwnerType { get; set; }
    public int OwnerId { get; set; }

    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? MessageId { get; set; }
    public Message? Message { get; set; }

    /// <summary>Imagem, vídeo, GIF ou sticker.</summary>
    public MediaKind MediaKind { get; set; } = MediaKind.Image;

    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }
    public string? Provider { get; set; }
    public string? ExternalId { get; set; }
    public string? StorageKey { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
