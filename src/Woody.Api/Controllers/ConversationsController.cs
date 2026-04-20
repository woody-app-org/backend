using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IDirectMessagingService _directMessaging;

    public ConversationsController(IDirectMessagingService directMessaging)
    {
        _directMessaging = directMessaging;
    }

    /// <summary>Inicia ou devolve a conversa DM com a outra utilizadora. O estado (pending/accepted) é decidido no servidor.</summary>
    [HttpPost]
    public async Task<ActionResult<ConversationResponseDto>> StartOrGet(
        [FromBody] StartConversationRequestDto body,
        CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _directMessaging.StartOrGetConversationAsync(me.Value, body.OtherUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Lista conversas ativas e pedidos enviados por ti a aguardar resposta (não inclui pedidos recebidos pendentes).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationResponseDto>>> ListMine(CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        return Ok(await _directMessaging.ListMyConversationsAsync(me.Value, cancellationToken));
    }

    /// <summary>Pedidos de conversa pendentes que recebeste (és a receptora, não a iniciadora).</summary>
    [HttpGet("pending-received")]
    public async Task<ActionResult<IReadOnlyList<ConversationResponseDto>>> ListPendingReceived(
        CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        return Ok(await _directMessaging.ListPendingRequestsReceivedAsync(me.Value, cancellationToken));
    }

    [HttpGet("{conversationId:int}")]
    public async Task<ActionResult<ConversationResponseDto>> GetById(
        int conversationId,
        CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _directMessaging.GetConversationForParticipantAsync(me.Value, conversationId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{conversationId:int}/accept")]
    public async Task<ActionResult<ConversationResponseDto>> Accept(int conversationId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _directMessaging.AcceptPendingAsync(me.Value, conversationId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{conversationId:int}/reject")]
    public async Task<ActionResult<ConversationResponseDto>> Reject(int conversationId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _directMessaging.RejectPendingAsync(me.Value, conversationId, cancellationToken);
        return Ok(result);
    }
}
