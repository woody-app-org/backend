using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Application.Mapping;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ICommunityMembershipRepository _memberships;
    private readonly IFollowRepository _follows;
    private readonly IPostRepository _posts;
    private readonly IPostEnrichmentService _postEnrichment;

    public UsersController(
        IUserRepository users,
        ICommunityMembershipRepository memberships,
        IFollowRepository follows,
        IPostRepository posts,
        IPostEnrichmentService postEnrichment)
    {
        _users = users;
        _memberships = memberships;
        _follows = follows;
        _posts = posts;
        _postEnrichment = postEnrichment;
    }

    [Authorize]
    [HttpGet("me/communities")]
    public async Task<ActionResult<List<string>>> GetMyCommunityIds(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var ids = await _memberships.GetActiveCommunityIdsAsStringsAsync(id.Value, cancellationToken);
        return Ok(ids);
    }

    [Authorize]
    [HttpGet("me/following")]
    public async Task<ActionResult<List<UserPublicDto>>> GetMyFollowing(CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var rows = await _follows.ListFollowingWithFollowedUserAsync(me.Value, cancellationToken);

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

        var followedIds = await _follows.GetFollowedUserIdsAsync(me.Value, cancellationToken);
        var exclude = followedIds.ToHashSet();
        exclude.Add(me.Value);

        var users = await _users.ListUsersForSuggestionsAsync(exclude, take, cancellationToken);

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

        var rows = await _memberships.ListActiveWithCommunityAndTagsByUserAsync(uid, cancellationToken);

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

        var user = await _users.GetByIdTrackedAsync(id.Value, cancellationToken);
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
            var existing = await _users.GetInterestsTrackedByUserIdAsync(user.Id, cancellationToken);
            _users.RemoveUserInterests(existing);
            foreach (var i in body.Interests)
            {
                _users.AddUserInterest(new UserInterest
                {
                    UserId = user.Id,
                    Label = i.Label.Trim()
                });
            }
        }

        await _users.SaveChangesAsync();

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

        var user = await _users.GetByIdTrackedAsync(id.Value, cancellationToken);
        if (user == null)
            return NotFound();

        var existing = await _users.GetInterestsTrackedByUserIdAsync(user.Id, cancellationToken);
        _users.RemoveUserInterests(existing);
        foreach (var i in body.Interests)
        {
            _users.AddUserInterest(new UserInterest
            {
                UserId = user.Id,
                Label = i.Label.Trim()
            });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();

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

        var (posts, total) = await _posts.ListByUserIdPagedAsync(uid, page, pageSize, cancellationToken);

        var items = await _postEnrichment.ToPostDtosAsync(posts, viewerId, cancellationToken);

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

        if (await _follows.ExistsAsync(me.Value, targetId, cancellationToken))
            return NoContent();

        _follows.Add(new Follow
        {
            FollowingUserId = me.Value,
            FollowedUserId = targetId,
            CreatedAt = DateTime.UtcNow
        });
        await _follows.SaveChangesAsync(cancellationToken);
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

        var row = await _follows.GetAsync(me.Value, targetId, cancellationToken);
        if (row != null)
        {
            _follows.Remove(row);
            await _follows.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private async Task<UserProfileDto?> BuildProfileAsync(int userId, int? viewerId, CancellationToken cancellationToken)
    {
        var u = await _users.GetByIdWithSocialLinksAndInterestsNoTrackingAsync(userId, cancellationToken);
        if (u == null)
            return null;

        bool? following = null;
        if (viewerId.HasValue && viewerId.Value != userId)
        {
            following = await _follows.ExistsAsync(viewerId.Value, userId, cancellationToken);
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
