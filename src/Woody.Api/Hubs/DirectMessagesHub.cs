using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Woody.Api.Extensions;
using Woody.Application.Interfaces;

namespace Woody.Api.Hubs;

/// <summary>
/// Tempo real para DMs e avisos de inbox da utilizadora autenticada. Grupos: <see cref="DirectMessageHubGroups.Conversation"/> (só após validação de participante)
/// e <see cref="DirectMessageHubGroups.UserInbox"/> (eventos como <c>inboxChanged</c> e <c>notificationsChanged</c>).
/// </summary>
[Authorize]
public sealed class DirectMessagesHub : Hub
{
    public const string RoutePath = "/hubs/direct-messages";

    private readonly IConversationRepository _conversations;

    public DirectMessagesHub(IConversationRepository conversations)
    {
        _conversations = conversations;
    }

    /// <summary>Subscrever atualizações da lista de conversas / pedidos da própria utilizadora.</summary>
    public async Task JoinUserInbox()
    {
        var uid = Context.User?.GetUserId();
        if (uid == null)
            throw new HubException("Sessão inválida.");

        await Groups.AddToGroupAsync(Context.ConnectionId, DirectMessageHubGroups.UserInbox(uid.Value));
    }

    public async Task LeaveUserInbox()
    {
        var uid = Context.User?.GetUserId();
        if (uid == null)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DirectMessageHubGroups.UserInbox(uid.Value));
    }

    /// <summary>Entrar no “room” da conversa (mensagens em tempo real). Só participantes.</summary>
    public async Task JoinConversation(int conversationId)
    {
        var uid = Context.User?.GetUserId();
        if (uid == null)
            throw new HubException("Sessão inválida.");

        if (!await _conversations.IsParticipantAsync(conversationId, uid.Value, Context.ConnectionAborted))
            throw new HubException("Sem acesso a esta conversa.");

        await Groups.AddToGroupAsync(Context.ConnectionId, DirectMessageHubGroups.Conversation(conversationId));
    }

    public async Task LeaveConversation(int conversationId)
    {
        var uid = Context.User?.GetUserId();
        if (uid == null)
            return;

        if (!await _conversations.IsParticipantAsync(conversationId, uid.Value, Context.ConnectionAborted))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DirectMessageHubGroups.Conversation(conversationId));
    }
}
