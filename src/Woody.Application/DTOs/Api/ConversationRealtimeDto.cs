namespace Woody.Application.DTOs.Api;

/// <summary>Payload neutro para eventos SignalR (evita campo <c>otherUser</c> dependente da vista).</summary>
public sealed class ConversationRealtimeDto
{
    public int Id { get; set; }

    /// <summary>pending | accepted | rejected</summary>
    public string Status { get; set; } = null!;

    public int UserLowId { get; set; }
    public int UserHighId { get; set; }
    public int? InitiatorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
