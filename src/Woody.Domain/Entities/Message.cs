namespace Woody.Domain.Entities;

/// <summary>
/// Mensagem numa conversa. <see cref="EditedAt"/> e <see cref="DeletedAt"/> suportam edição e exclusão lógica.
/// </summary>
public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int SenderUserId { get; set; }
    public User Sender { get; set; } = null!;

    /// <summary>Texto; pode ser vazio quando existirem apenas anexos.</summary>
    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<MediaAttachment> MediaAttachments { get; set; } = new List<MediaAttachment>();
}
