using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile-signals")]
[EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
public class ProfileSignalsController : ControllerBase
{
    private readonly IProfileSignalService _profileSignals;

    public ProfileSignalsController(IProfileSignalService profileSignals)
    {
        _profileSignals = profileSignals;
    }

    [HttpPost]
    public async Task<ActionResult<ProfileSignalResponseDto>> Send(
        [FromBody] SendProfileSignalRequestDto body,
        CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var receiverUserId = ResolveReceiverUserId(body);
        var result = await _profileSignals.SendAsync(me.Value, receiverUserId, body.Type, body.Message, cancellationToken);
        if (result.Outcome != ProfileSignalOperationOutcome.Success)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetStatus), new { receiverUserId, type = body.Type }, result.Signal);
    }

    [HttpGet("received")]
    public async Task<ActionResult<PaginatedResponseDto<ProfileSignalResponseDto>>> ListReceived(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        return Ok(await _profileSignals.ListReceivedAsync(me.Value, page, pageSize, cancellationToken));
    }

    /// <summary>Contagem de sinais recebidos ainda não lidos (estado enviado). Para badge/indicador na área privada.</summary>
    [HttpGet("received/unread-count")]
    public async Task<ActionResult<ProfileSignalsUnreadCountDto>> GetReceivedUnreadCount(
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        return Ok(await _profileSignals.GetUnreadReceivedCountAsync(me.Value, cancellationToken));
    }

    [HttpGet("sent")]
    public async Task<ActionResult<PaginatedResponseDto<ProfileSignalResponseDto>>> ListSent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        return Ok(await _profileSignals.ListSentAsync(me.Value, page, pageSize, cancellationToken));
    }

    [HttpGet("status")]
    public async Task<ActionResult<ProfileSignalStatusResponseDto>> GetStatus(
        [FromQuery] int receiverUserId,
        [FromQuery] int recipientUserId = 0,
        [FromQuery] string type = "te_notei",
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var resolvedReceiverUserId = receiverUserId > 0 ? receiverUserId : recipientUserId;
        return Ok(await _profileSignals.GetSendStatusAsync(me.Value, resolvedReceiverUserId, type, cancellationToken));
    }

    [HttpPatch("{signalId:int}/archive")]
    public async Task<ActionResult<ProfileSignalResponseDto>> Archive(int signalId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _profileSignals.ArchiveAsync(me.Value, signalId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{signalId:int}/read")]
    public async Task<ActionResult<ProfileSignalResponseDto>> MarkRead(int signalId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _profileSignals.MarkReadAsync(me.Value, signalId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{signalId:int}/dismiss")]
    public async Task<ActionResult<ProfileSignalResponseDto>> Dismiss(int signalId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _profileSignals.DismissAsync(me.Value, signalId, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<ProfileSignalResponseDto> ToActionResult(ProfileSignalCommandResult result) =>
        result.Outcome switch
        {
            ProfileSignalOperationOutcome.Success => Ok(result.Signal),
            ProfileSignalOperationOutcome.InvalidType => BadRequest(new { error = result.Error }),
            ProfileSignalOperationOutcome.InvalidReceiver => BadRequest(new { error = result.Error }),
            ProfileSignalOperationOutcome.SelfSignal => BadRequest(new { error = result.Error }),
            ProfileSignalOperationOutcome.ReceiverNotFound => NotFound(new { error = result.Error }),
            ProfileSignalOperationOutcome.CooldownActive => Conflict(new
            {
                error = result.Error,
                nextAllowedAt = result.NextAllowedAt.HasValue ? EntityMappers.Iso(result.NextAllowedAt.Value) : null
            }),
            ProfileSignalOperationOutcome.NotFound => NotFound(new { error = result.Error }),
            ProfileSignalOperationOutcome.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error ?? "Não foi possível concluir a operação." })
        };

    private static int ResolveReceiverUserId(SendProfileSignalRequestDto body) =>
        body.ReceiverUserId > 0 ? body.ReceiverUserId : body.RecipientUserId;
}
