namespace Woody.Application.DTOs.Api;

/// <summary>Evento leve para a lista de conversas (inbox) sem payload completo da mensagem.</summary>
public sealed class DirectMessageInboxEventDto
{
    /// <summary>Ex.: "message" | "conversation".</summary>
    public string Kind { get; set; } = null!;

    public int ConversationId { get; set; }
}
