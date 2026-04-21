namespace Woody.Domain.Entities;

/// <summary>
/// Anexo de imagem (ou outro ficheiro) associado a uma mensagem. Modelo alinhado a <see cref="PostImage"/> (URL + storage).
/// </summary>
public class MessageAttachment
{
    public int Id { get; set; }

    public int MessageId { get; set; }
    public Message Message { get; set; } = null!;

    public string Url { get; set; } = null!;

    /// <summary>Chave no blob/storage quando o upload for implementado.</summary>
    public string? StorageKey { get; set; }

    /// <summary>MIME, ex. image/jpeg (opcional).</summary>
    public string? ContentType { get; set; }

    /// <summary>Ordem de exibição (0 = primeira).</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}
