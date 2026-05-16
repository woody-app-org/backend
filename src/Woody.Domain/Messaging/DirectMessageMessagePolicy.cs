using Woody.Domain.Entities;

namespace Woody.Domain.Messaging;

/// <summary>Regras de mensagens DM (edição, exclusão). Envio continua em <see cref="DirectMessageConversationPolicy"/>.</summary>
public static class DirectMessageMessagePolicy
{
    public static bool MayEditOrSoftDelete(Message message, int userId) =>
        message.DeletedAt == null && message.SenderUserId == userId;
}
