using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Mapping;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly WoodyDbContext _db;
    private readonly IUserRepository _users;

    public UsersController(WoodyDbContext db, IUserRepository users)
    {
        _db = db;
        _users = users;
    }

    [Authorize]
    [HttpGet("me/communities")]
    public async Task<ActionResult<List<string>>> GetMyCommunityIds(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var ids = await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.UserId == id.Value && m.Status == "active")
            .Select(m => m.CommunityId.ToString())
            .ToListAsync(cancellationToken);
        return Ok(ids);
    }

    [Authorize]
    [HttpGet("me/following")]
    public async Task<ActionResult<List<UserPublicDto>>> GetMyFollowing(CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var rows = await _db.Follows.AsNoTracking()
            .Where(f => f.FollowingUserId == me.Value)
            .Include(f => f.FollowedUser)
            .OrderBy(f => f.FollowedUser.DisplayName ?? f.FollowedUser.Username)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(f => EntityMappers.ToUserPublicDto(f.FollowedUser)).ToList());
    }

    [Authorize]
    [HttpGet("me/suggestions")]
    public async Task<ActionResult<List<UserPublicDto>>> GetSuggestions(
        [FromQuery] int take = 8,
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        take = Math.Clamp(take, 1, 50);

        var followedIds = await _db.Follows.AsNoTracking()
            .Where(f => f.FollowingUserId == me.Value)
            .Select(f => f.FollowedUserId)
            .ToListAsync(cancellationToken);
        var exclude = followedIds.ToHashSet();
        exclude.Add(me.Value);

        var users = await _db.Users.AsNoTracking()
            .Where(u => !exclude.Contains(u.Id))
            .OrderBy(u => u.DisplayName ?? u.Username)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(EntityMappers.ToUserPublicDto).ToList());
    }

    [AllowAnonymous]
    [HttpGet("{userId}/communities")]
    public async Task<ActionResult<List<UserCommunityMembershipDto>>> GetUserCommunities(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var uid))
            return BadRequest();

        var rows = await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.UserId == uid && m.Status == "active")
            .Include(m => m.Community)
            .ThenInclude(c => c.Tags)
            .ToListAsync(cancellationToken);

        var list = rows
            .OrderBy(m => m.Community.Name)
            .Select(m => new UserCommunityMembershipDto
            {
                Community = EntityMappers.ToCommunityDto(m.Community),
                Role = m.Role
            })
            .ToList();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var profile = await BuildProfileAsync(id.Value, viewerId: id.Value, cancellationToken);
        return profile == null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<ActionResult<UserProfileDto>> PatchMe(
        [FromBody] UpdateProfileRequestDTO body,
        CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
        if (user == null)
            return NotFound();

        if (!string.Equals(user.Username, body.Username, StringComparison.Ordinal))
        {
            if (await _users.ExistsUsernameAsync(body.Username.Trim()))
                return Conflict(new { error = "Nome de utilizador já existe." });
            user.Username = body.Username.Trim();
        }

        user.DisplayName = body.Name.Trim();
        user.Bio = body.Bio;
        user.Pronouns = body.Pronouns;
        user.Location = body.Location;
        user.ProfilePic = body.AvatarUrl;
        user.BannerPic = body.BannerUrl;
        user.UpdatedAt = DateTime.UtcNow;

        if (body.Interests != null)
        {
            var existing = await _db.UserInterests.Where(i => i.UserId == user.Id).ToListAsync(cancellationToken);
            _db.UserInterests.RemoveRange(existing);
            foreach (var i in body.Interests)
            {
                _db.UserInterests.Add(new UserInterest
                {
                    UserId = user.Id,
                    Label = i.Label.Trim()
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var profile = await BuildProfileAsync(user.Id, viewerId: id.Value, cancellationToken);
        return Ok(profile);
    }

    [Authorize]
    [HttpPatch("me/interests")]
    public async Task<ActionResult<UserProfileDto>> PatchInterests(
        [FromBody] UpdateInterestsRequestDTO body,
        CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
        if (user == null)
            return NotFound();

        var existing = await _db.UserInterests.Where(i => i.UserId == user.Id).ToListAsync(cancellationToken);
        _db.UserInterests.RemoveRange(existing);
        foreach (var i in body.Interests)
        {
            _db.UserInterests.Add(new UserInterest
            {
                UserId = user.Id,
                Label = i.Label.Trim()
            });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var profile = await BuildProfileAsync(user.Id, viewerId: id.Value, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserProfileDto>> GetById(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var uid))
            return BadRequest();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var profile = await BuildProfileAsync(uid, viewerId, cancellationToken);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpGet("{userId}/posts")]
    public async Task<ActionResult<PaginatedResponseDto<PostResponseDto>>> GetUserPosts(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var uid))
            return BadRequest();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var q = _db.Posts.AsNoTracking()
            .Where(p => p.UserId == uid && p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags);

        var total = await q.CountAsync(cancellationToken);
        var posts = await q
            .OrderByDescending(p => p.CreatedAt)
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
    [HttpPost("{userId}/follow")]
    public async Task<IActionResult> Follow(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var targetId))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null || me.Value == targetId)
            return BadRequest();

        if (await _db.Follows.AnyAsync(f => f.FollowingUserId == me.Value && f.FollowedUserId == targetId, cancellationToken))
            return NoContent();

        _db.Follows.Add(new Follow
        {
            FollowingUserId = me.Value,
            FollowedUserId = targetId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{userId}/follow")]
    public async Task<IActionResult> Unfollow(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var targetId))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var row = await _db.Follows.FirstOrDefaultAsync(
            f => f.FollowingUserId == me.Value && f.FollowedUserId == targetId,
            cancellationToken);
        if (row != null)
        {
            _db.Follows.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private async Task<UserProfileDto?> BuildProfileAsync(int userId, int? viewerId, CancellationToken cancellationToken)
    {
        var u = await _db.Users.AsNoTracking()
            .Include(x => x.SocialLinks)
            .Include(x => x.Interests)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (u == null)
            return null;

        bool? following = null;
        if (viewerId.HasValue && viewerId.Value != userId)
        {
            following = await _db.Follows.AnyAsync(
                f => f.FollowingUserId == viewerId.Value && f.FollowedUserId == userId,
                cancellationToken);
        }

        var links = u.SocialLinks.Select(s => new SocialLinkDto
        {
            Id = s.Id.ToString(),
            Platform = s.Platform,
            Label = s.Label,
            Url = s.Url,
            Handle = s.Handle
        }).ToList();

        var interests = u.Interests.Select(i => new InterestItemResponseDto
        {
            Id = i.Id.ToString(),
            Label = i.Label
        }).ToList();

        return EntityMappers.ToUserProfile(u, following, links, interests);
    }
}
