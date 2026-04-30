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

    /// <summary>Lista notificações da utilizadora autenticada (mais recentes primeiro).</summary>
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

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

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

    /// <summary>Marca todas as notificações como lidas.</summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        await _notifications.MarkAllReadAsync(me.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>Marca uma notificação como lida (apenas se pertencer à utilizadora).</summary>
    [HttpPatch("{notificationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int notificationId, CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var ok = await _notifications.TryMarkReadAsync(me.Value, notificationId, cancellationToken);
        if (!ok)
            return NotFound(new { error = "Notificação não encontrada." });

        return NoContent();
    }
}
