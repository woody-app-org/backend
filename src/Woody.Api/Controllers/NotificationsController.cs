using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
[EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    [ProducesResponseType(typeof(NotificationListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListResponseDto>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _notifications.ListMineAsync(me.Value, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(NotificationUnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountDto>> UnreadCount(CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var count = await _notifications.GetUnreadCountAsync(me.Value, cancellationToken);
        return Ok(new NotificationUnreadCountDto { Count = count });
    }

    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkRead(string notificationId, CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!int.TryParse(notificationId, out var nid))
            return BadRequest();

        var ok = await _notifications.TryMarkReadAsync(me.Value, nid, cancellationToken);
        if (!ok)
            return NotFound();

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        await _notifications.MarkAllReadAsync(me.Value, cancellationToken);
        return NoContent();
    }
}
