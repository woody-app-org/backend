using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Validation;
using Woody.Domain.Entities;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize(Policy = "VerifiedAccount")]
[Route("api/join-requests")]
public class JoinRequestsController : ControllerBase
{
    private readonly IJoinRequestRepository _joinRequests;
    private readonly ICommunityMembershipRepository _memberships;
    private readonly ICommunityPermissionService _permission;
    private readonly INotificationService _notificationService;

    public JoinRequestsController(
        IJoinRequestRepository joinRequests,
        ICommunityMembershipRepository memberships,
        ICommunityPermissionService permission,
        INotificationService notificationService)
    {
        _joinRequests = joinRequests;
        _memberships = memberships;
        _permission = permission;
        _notificationService = notificationService;
    }

    [Authorize]
    [HttpPost("{joinRequestId}/approve")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> Approve(string joinRequestId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(joinRequestId, out var jrid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var jr = await _joinRequests.GetWithCommunityTrackedAsync(jrid, cancellationToken);
        if (jr == null || jr.Status != "pending")
            return NotFound();

        if (!await _permission.CanModerateCommunityAsync(jr.CommunityId, me.Value, cancellationToken))
            return Forbid();

        var membership = await _memberships.GetForUserAndCommunityAsync(jr.UserId, jr.CommunityId, cancellationToken);
        if (MembershipIsBanned(membership))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "membership_banned",
                error = "Esta conta está restrita nesta comunidade; não é possível aprovar o pedido por este fluxo."
            });
        }

        var now = DateTime.UtcNow;
        jr.Status = "approved";
        jr.ReviewedAt = now;
        jr.ReviewedByUserId = me.Value;
        jr.RejectionReason = null;
        jr.UpdatedAt = now;

        if (membership == null)
        {
            _memberships.Add(new CommunityMembership
            {
                UserId = jr.UserId,
                CommunityId = jr.CommunityId,
                Role = "member",
                Status = "active",
                JoinedAt = now
            });
        }
        else
        {
            membership.Status = "active";
            membership.JoinedAt ??= now;
        }

        jr.Community.MemberCount = await _memberships.CountActiveInCommunityAsync(jr.CommunityId, cancellationToken);
        jr.Community.UpdatedAt = now;

        await _joinRequests.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyCommunityRequestApprovedAsync(
            jr.UserId,
            me.Value,
            jr.CommunityId,
            jr.Community.Slug,
            jr.Id,
            cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{joinRequestId}/reject")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> Reject(
        string joinRequestId,
        [FromBody] JoinRequestRejectRequestDTO? body,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(joinRequestId, out var jrid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var jr = await _joinRequests.GetWithCommunityTrackedAsync(jrid, cancellationToken);
        if (jr == null || jr.Status != "pending")
            return NotFound();

        if (!await _permission.CanModerateCommunityAsync(jr.CommunityId, me.Value, cancellationToken))
            return Forbid();

        if (!InputValidator.TryNormalizeOptionalText(
                body?.Reason,
                "Motivo",
                InputValidationLimits.JoinRequestRejectionReasonMaxLength,
                out var reason,
                out var error))
            return BadRequest(new { error });

        var now = DateTime.UtcNow;
        jr.Status = "rejected";
        jr.ReviewedAt = now;
        jr.ReviewedByUserId = me.Value;
        jr.RejectionReason = reason;
        jr.UpdatedAt = now;
        await _joinRequests.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool MembershipIsBanned(CommunityMembership? m) =>
        m != null && string.Equals(m.Status, "banned", StringComparison.OrdinalIgnoreCase);
}
