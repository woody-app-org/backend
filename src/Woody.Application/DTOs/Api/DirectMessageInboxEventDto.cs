namespace Woody.Application.DTOs.Api;

/// <summary>
/// Evento leve para a lista de conversas (inbox) sem payload completo da mensagem.
/// Contrato estável para o cliente; evoluções futuras (lido, reações, arquivo) podem acrescentar <c>Kind</c> ou campos opcionais.
/// </summary>
public sealed class DirectMessageInboxEventDto
{
    /// <summary><c>message</c> — atividade numa conversa; <c>conversation</c> — estado do pedido / metadados.</summary>
    public string Kind { get; set; } = null!;

    public int ConversationId { get; set; }
}
