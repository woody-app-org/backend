using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/join-requests")]
public class JoinRequestsController : ControllerBase
{
    private readonly IJoinRequestRepository _joinRequests;
    private readonly ICommunityMembershipRepository _memberships;
    private readonly ICommunityPermissionService _permission;

    public JoinRequestsController(
        IJoinRequestRepository joinRequests,
        ICommunityMembershipRepository memberships,
        ICommunityPermissionService permission)
    {
        _joinRequests = joinRequests;
        _memberships = memberships;
        _permission = permission;
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

        jr.Status = "approved";

        var membership = await _memberships.GetForUserAndCommunityAsync(jr.UserId, jr.CommunityId, cancellationToken);
        if (membership == null)
        {
            _memberships.Add(new CommunityMembership
            {
                UserId = jr.UserId,
                CommunityId = jr.CommunityId,
                Role = "member",
                Status = "active",
                JoinedAt = DateTime.UtcNow
            });
        }
        else
        {
            membership.Status = "active";
            membership.JoinedAt ??= DateTime.UtcNow;
        }

        jr.Community.MemberCount = await _memberships.CountActiveInCommunityAsync(jr.CommunityId, cancellationToken);
        jr.Community.UpdatedAt = DateTime.UtcNow;

        await _joinRequests.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{joinRequestId}/reject")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> Reject(string joinRequestId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(joinRequestId, out var jrid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var jr = await _joinRequests.GetTrackedAsync(jrid, cancellationToken);
        if (jr == null || jr.Status != "pending")
            return NotFound();

        if (!await _permission.CanModerateCommunityAsync(jr.CommunityId, me.Value, cancellationToken))
            return Forbid();

        jr.Status = "rejected";
        await _joinRequests.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
