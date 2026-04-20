using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Messaging;

/// <summary>
/// Regras centrais de conversas diretas (fonte de verdade no domínio). Serviços da API devem delegar aqui
/// para não duplicar condições de pedido, aceite e envio de mensagens.
/// </summary>
public static class DirectMessageConversationPolicy
{
    /// <summary>Normaliza o par (A,B) para (low, high) com low &lt; high.</summary>
    /// <exception cref="ArgumentException">Mesma utilizadora ou identificadores inválidos.</exception>
    public static (int UserLowId, int UserHighId) OrderParticipantPair(int userIdA, int userIdB)
    {
        if (userIdA <= 0 || userIdB <= 0)
            throw new ArgumentOutOfRangeException(nameof(userIdA), "Ids de utilizadora devem ser positivos.");
        if (userIdA == userIdB)
            throw new ArgumentException("Uma conversa DM requer duas utilizadoras distintas.", nameof(userIdB));

        return userIdA < userIdB ? (userIdA, userIdB) : (userIdB, userIdA);
    }

    /// <summary>Estado inicial: aceite imediatamente com follow mútuo; caso contrário pedido pendente.</summary>
    public static ConversationStatus InitialStatus(bool mutualFollow) =>
        mutualFollow ? ConversationStatus.Accepted : ConversationStatus.Pending;

    /// <summary>
    /// Iniciador do pedido quando não há follow mútuo (quem envia a primeira interação). Null quando a conversa já nasce aceite.
    /// </summary>
    public static int? InitialInitiatorUserId(bool mutualFollow, int startingUserId) =>
        mutualFollow ? null : startingUserId;

    public static bool IsParticipant(Conversation conversation, int userId) =>
        conversation.UserLowId == userId || conversation.UserHighId == userId;

    /// <summary>
    /// Enquanto <see cref="ConversationStatus.Pending"/>, só a iniciadora pode enviar mensagens; após aceite, ambas.
    /// Conversas recusadas não permitem envio.
    /// </summary>
    public static bool MaySendMessage(Conversation conversation, int senderUserId)
    {
        if (conversation.Status == ConversationStatus.Rejected)
            return false;
        if (!IsParticipant(conversation, senderUserId))
            return false;
        if (conversation.Status == ConversationStatus.Accepted)
            return true;

        return conversation.InitiatorUserId == senderUserId;
    }

    /// <summary>A outra participante (não iniciadora) pode aceitar ou recusar o pedido.</summary>
    public static bool MayAcceptOrRejectRequest(Conversation conversation, int actingUserId)
    {
        if (conversation.Status != ConversationStatus.Pending)
            return false;
        if (!conversation.InitiatorUserId.HasValue)
            return false;
        if (actingUserId == conversation.InitiatorUserId.Value)
            return false;

        return IsParticipant(conversation, actingUserId);
    }
}
