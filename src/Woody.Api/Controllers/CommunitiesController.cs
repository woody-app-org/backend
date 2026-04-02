using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Infrastructure.Mapping;
using Woody.Infrastructure.Persistence.Context;
using Woody.Api;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/communities")]
public class CommunitiesController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public CommunitiesController(WoodyDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<CommunityResponseDto>>> List(CancellationToken cancellationToken)
    {
        var list = await _db.Communities.AsNoTracking()
            .Include(c => c.Tags)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return Ok(list.Select(EntityMappers.ToCommunityDto).ToList());
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommunityResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var c = await _db.Communities.AsNoTracking()
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return c == null ? NotFound() : Ok(EntityMappers.ToCommunityDto(c));
    }

    [AllowAnonymous]
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<CommunityResponseDto>> BySlug(string slug, CancellationToken cancellationToken)
    {
        var c = await _db.Communities.AsNoTracking()
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        return c == null ? NotFound() : Ok(EntityMappers.ToCommunityDto(c));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/posts")]
    public async Task<ActionResult<PaginatedResponseDto<PostResponseDto>>> CommunityPosts(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var q = _db.Posts.AsNoTracking()
            .Where(p => p.CommunityId == id && p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .OrderByDescending(p => p.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var posts = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await PostEnricher.ToPostDtosAsync(_db, posts, viewerId, cancellationToken);

        return Ok(new PaginatedResponseDto<PostResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        });
    }

    [Authorize]
    [HttpGet("{id:int}/join-requests")]
    public async Task<IActionResult> PendingJoinRequests(int id, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await IsAdminOrOwnerAsync(id, me.Value, cancellationToken))
            return Forbid();

        var rows = await _db.JoinRequests.AsNoTracking()
            .Where(j => j.CommunityId == id && j.Status == "pending")
            .Include(j => j.User)
            .OrderBy(j => j.RequestedAt)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(j => new
        {
            id = j.Id.ToString(),
            communityId = j.CommunityId.ToString(),
            userId = j.UserId.ToString(),
            status = j.Status,
            requestedAt = j.RequestedAt.ToUniversalTime().ToString("o"),
            user = EntityMappers.ToUserPublicDto(j.User)
        }));
    }

    [AllowAnonymous]
    [HttpGet("{communityId}/members")]
    public async Task<ActionResult<List<CommunityMemberItemDto>>> Members(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var rows = await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.CommunityId == cid && m.Status == "active")
            .Include(m => m.User)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(m => new CommunityMemberItemDto
        {
            User = EntityMappers.ToUserPublicDto(m.User),
            Role = m.Role
        }).ToList());
    }

    [Authorize]
    [HttpPatch("{communityId}")]
    public async Task<ActionResult<CommunityResponseDto>> Patch(
        string communityId,
        [FromBody] CommunityUpdateRequestDTO body,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await IsAdminOrOwnerAsync(cid, me.Value, cancellationToken))
            return Forbid();

        var c = await _db.Communities.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == cid, cancellationToken);
        if (c == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(body.Name))
            c.Name = body.Name.Trim();
        if (body.Description != null)
            c.Description = body.Description;
        if (!string.IsNullOrWhiteSpace(body.Category))
            c.Category = body.Category.Trim();
        if (body.Rules != null)
            c.Rules = body.Rules;
        if (!string.IsNullOrWhiteSpace(body.Visibility))
            c.Visibility = body.Visibility.Trim();
        if (body.AvatarUrl != null)
            c.AvatarUrl = string.IsNullOrWhiteSpace(body.AvatarUrl) ? null : body.AvatarUrl.Trim();
        if (body.CoverUrl != null)
            c.CoverUrl = string.IsNullOrWhiteSpace(body.CoverUrl) ? null : body.CoverUrl.Trim();

        if (body.Tags != null)
        {
            _db.CommunityTags.RemoveRange(c.Tags);
            foreach (var t in body.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
                _db.CommunityTags.Add(new CommunityTag { CommunityId = c.Id, Tag = t.Trim() });
        }

        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        c = await _db.Communities.AsNoTracking().Include(x => x.Tags).FirstAsync(x => x.Id == cid, cancellationToken);
        return Ok(EntityMappers.ToCommunityDto(c));
    }

    [Authorize]
    [HttpPost("{communityId}/members")]
    public async Task<IActionResult> JoinPublic(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var c = await _db.Communities.FirstOrDefaultAsync(x => x.Id == cid, cancellationToken);
        if (c == null)
            return NotFound();

        if (!string.Equals(c.Visibility, "public", StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        await EnsureMembershipAsync(me.Value, c, active: true, role: "member", cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{communityId}/join-requests")]
    public async Task<IActionResult> RequestJoin(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var c = await _db.Communities.FirstOrDefaultAsync(x => x.Id == cid, cancellationToken);
        if (c == null)
            return NotFound();

        if (string.Equals(c.Visibility, "public", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureMembershipAsync(me.Value, c, active: true, role: "member", cancellationToken);
            return NoContent();
        }

        var existingMember = await _db.CommunityMemberships.FirstOrDefaultAsync(
            m => m.CommunityId == cid && m.UserId == me.Value,
            cancellationToken);
        if (existingMember is { Status: "active" })
            return NoContent();

        var pendingJr = await _db.JoinRequests.AnyAsync(
            j => j.CommunityId == cid && j.UserId == me.Value && j.Status == "pending",
            cancellationToken);
        if (pendingJr)
            return NoContent();

        _db.JoinRequests.Add(new JoinRequest
        {
            CommunityId = cid,
            UserId = me.Value,
            Status = "pending",
            RequestedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{communityId}/join")]
    public Task<IActionResult> JoinAlias(string communityId, CancellationToken cancellationToken) =>
        RequestJoin(communityId, cancellationToken);

    [Authorize]
    [HttpDelete("{communityId}/members/me")]
    public async Task<IActionResult> Leave(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var m = await _db.CommunityMemberships.FirstOrDefaultAsync(
            x => x.CommunityId == cid && x.UserId == me.Value,
            cancellationToken);
        if (m == null)
            return NoContent();

        if (m.Role == "owner")
            return BadRequest();

        _db.CommunityMemberships.Remove(m);
        await _db.SaveChangesAsync(cancellationToken);
        var c = await _db.Communities.FirstAsync(x => x.Id == cid, cancellationToken);
        c.MemberCount = await _db.CommunityMemberships.CountAsync(
            x => x.CommunityId == cid && x.Status == "active",
            cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{communityId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string communityId, string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid) || !int.TryParse(userId, out var uid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await IsAdminOrOwnerAsync(cid, me.Value, cancellationToken))
            return Forbid();

        var m = await _db.CommunityMemberships.FirstOrDefaultAsync(
            x => x.CommunityId == cid && x.UserId == uid,
            cancellationToken);
        if (m == null)
            return NoContent();

        _db.CommunityMemberships.Remove(m);
        await _db.SaveChangesAsync(cancellationToken);
        var c = await _db.Communities.FirstAsync(x => x.Id == cid, cancellationToken);
        c.MemberCount = await _db.CommunityMemberships.CountAsync(
            x => x.CommunityId == cid && x.Status == "active",
            cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPatch("{communityId}/members/{userId}")]
    public async Task<IActionResult> PatchMember(
        string communityId,
        string userId,
        [FromBody] MembershipPatchRequestDTO body,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid) || !int.TryParse(userId, out var uid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await IsAdminOrOwnerAsync(cid, me.Value, cancellationToken))
            return Forbid();

        var m = await _db.CommunityMemberships.FirstOrDefaultAsync(
            x => x.CommunityId == cid && x.UserId == uid,
            cancellationToken);
        if (m == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(body.Status))
            m.Status = body.Status.Trim();
        if (!string.IsNullOrWhiteSpace(body.Role))
            m.Role = body.Role.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        var comm = await _db.Communities.FirstAsync(x => x.Id == cid, cancellationToken);
        comm.MemberCount = await _db.CommunityMemberships.CountAsync(
            x => x.CommunityId == cid && x.Status == "active",
            cancellationToken);
        comm.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> IsAdminOrOwnerAsync(int communityId, int userId, CancellationToken cancellationToken)
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

    private async Task EnsureMembershipAsync(int userId, Community c, bool active, string role, CancellationToken cancellationToken)
    {
        var existing = await _db.CommunityMemberships.FirstOrDefaultAsync(
            m => m.CommunityId == c.Id && m.UserId == userId,
            cancellationToken);
        if (existing != null)
        {
            if (active)
            {
                existing.Status = "active";
                existing.Role = existing.Role == "owner" ? "owner" : role;
                existing.JoinedAt ??= DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            c.MemberCount = await _db.CommunityMemberships.CountAsync(
                m => m.CommunityId == c.Id && m.Status == "active",
                cancellationToken);
            c.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.CommunityMemberships.Add(new CommunityMembership
        {
            UserId = userId,
            CommunityId = c.Id,
            Role = role,
            Status = active ? "active" : "pending",
            JoinedAt = active ? DateTime.UtcNow : null
        });
        await _db.SaveChangesAsync(cancellationToken);
        c.MemberCount = await _db.CommunityMemberships.CountAsync(
            m => m.CommunityId == c.Id && m.Status == "active",
            cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
