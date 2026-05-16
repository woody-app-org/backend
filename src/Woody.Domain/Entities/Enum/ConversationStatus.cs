namespace Woody.Domain.Entities.Enum;

/// <summary>
/// Estado da conversa 1:1. <see cref="Pending"/> = solicitação aguardando resposta da outra participante;
/// <see cref="Accepted"/> = chat ativo (follow mútuo na criação ou pedido aceite); <see cref="Rejected"/> = pedido recusado.
/// </summary>
public enum ConversationStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3
}
