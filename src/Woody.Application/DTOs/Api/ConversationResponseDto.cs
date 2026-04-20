namespace Woody.Application.DTOs.Api;

/// <summary>Conversa DM para a API. <see cref="Status"/> espelha o enum de domínio em minúsculas para o frontend.</summary>
public sealed class ConversationResponseDto
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

    /// <summary>A outra participante face à utilizadora autenticada.</summary>
    public ConversationPeerPreviewDto OtherUser { get; set; } = null!;

    /// <summary>Prévia curta da última mensagem não apagada (texto truncado ou “Imagem”).</summary>
    public string? LastMessagePreview { get; set; }

    /// <summary>Instante da última mensagem não apagada, para ordenação na inbox.</summary>
    public DateTime? LastMessageAt { get; set; }
}
