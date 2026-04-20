using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Conversa direta entre duas utilizadoras. O par ordenado (<see cref="UserLowId"/>, <see cref="UserHighId"/>)
/// garante unicidade na base de dados (sempre user_low_id &lt; user_high_id).
/// </summary>
public class Conversation
{
    public int Id { get; set; }

    public int UserLowId { get; set; }
    public User UserLow { get; set; } = null!;

    public int UserHighId { get; set; }
    public User UserHigh { get; set; } = null!;

    /// <summary>
    /// Quem iniciou o contacto quando <see cref="Status"/> é <see cref="ConversationStatus.Pending"/>; null quando
    /// a conversa nasce <see cref="ConversationStatus.Accepted"/> por follow mútuo (sem fluxo de pedido).
    /// </summary>
    public int? InitiatorUserId { get; set; }
    public User? Initiator { get; set; }

    public ConversationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Momento em que a receptora aceitou ou recusou (quando aplicável).</summary>
    public DateTime? RespondedAt { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
