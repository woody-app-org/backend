using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/join-requests")]
public class JoinRequestsController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public JoinRequestsController(WoodyDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpPost("{joinRequestId}/approve")]
    public async Task<IActionResult> Approve(string joinRequestId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(joinRequestId, out var jrid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var jr = await _db.JoinRequests.Include(j => j.Community).FirstOrDefaultAsync(j => j.Id == jrid, cancellationToken);
        if (jr == null || jr.Status != "pending")
            return NotFound();

        if (!await CanModerateAsync(jr.CommunityId, me.Value, cancellationToken))
            return Forbid();

        jr.Status = "approved";

        var membership = await _db.CommunityMemberships.FirstOrDefaultAsync(
            m => m.CommunityId == jr.CommunityId && m.UserId == jr.UserId,
            cancellationToken);
        if (membership == null)
        {
            _db.CommunityMemberships.Add(new CommunityMembership
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

        var c = await _db.Communities.FirstAsync(x => x.Id == jr.CommunityId, cancellationToken);
        c.MemberCount = await _db.CommunityMemberships.CountAsync(
            m => m.CommunityId == c.Id && m.Status == "active",
            cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{joinRequestId}/reject")]
    public async Task<IActionResult> Reject(string joinRequestId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(joinRequestId, out var jrid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var jr = await _db.JoinRequests.FirstOrDefaultAsync(j => j.Id == jrid, cancellationToken);
        if (jr == null || jr.Status != "pending")
            return NotFound();

        if (!await CanModerateAsync(jr.CommunityId, me.Value, cancellationToken))
            return Forbid();

        jr.Status = "rejected";
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanModerateAsync(int communityId, int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        var m = await _db.CommunityMemberships.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CommunityId == communityId && x.UserId == userId && x.Status == "active",
                cancellationToken);
        return m is { Role: "owner" or "admin" };
    }
}
