namespace Woody.Domain.Entities;

/// <summary>
/// Participação na conversa (sempre duas linhas por conversa DM). Permite estado por utilizadora (leitura, etc.).
/// </summary>
public class ConversationParticipant
{
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    /// <summary>Última marcação de leitura na conversa (evolução futura: sincronizar com <see cref="LastReadMessageId"/>).</summary>
    public DateTime? LastReadAt { get; set; }

    /// <summary>Última mensagem considerada lida por esta utilizadora.</summary>
    public int? LastReadMessageId { get; set; }
}
