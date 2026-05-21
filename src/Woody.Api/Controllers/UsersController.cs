using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Application.Mapping;
using Woody.Application.Services;
using Woody.Application.Validation;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IUsernameHistoryRepository _usernameHistory;
    private readonly UsernameResolver _usernameResolver;
    private readonly ICommunityMembershipRepository _memberships;
    private readonly IFollowRepository _follows;
    private readonly IPostRepository _posts;
    private readonly IPostEnrichmentService _postEnrichment;
    private readonly INotificationService _notificationService;
    private readonly IStoryRepository _stories;

    public UsersController(
        IUserRepository users,
        IUsernameHistoryRepository usernameHistory,
        UsernameResolver usernameResolver,
        ICommunityMembershipRepository memberships,
        IFollowRepository follows,
        IPostRepository posts,
        IPostEnrichmentService postEnrichment,
        INotificationService notificationService,
        IStoryRepository stories)
    {
        _users = users;
        _usernameHistory = usernameHistory;
        _usernameResolver = usernameResolver;
        _memberships = memberships;
        _follows = follows;
        _posts = posts;
        _postEnrichment = postEnrichment;
        _notificationService = notificationService;
        _stories = stories;
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("me/communities")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<List<string>>> GetMyCommunityIds(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var ids = await _memberships.GetActiveCommunityIdsAsStringsAsync(id.Value, cancellationToken);
        return Ok(ids);
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("me/following")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<PaginatedResponseDto<UserPublicDto>>> GetMyFollowing(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _follows.ListFollowingPagedAsync(me.Value, page, pageSize, cancellationToken);
        var storyFlags = await _stories.GetUserIdsWithActiveStoriesAsync(
            items.Select(u => u.Id),
            cancellationToken);

        return Ok(new PaginatedResponseDto<UserPublicDto>
        {
            Items = items.Select(u => EntityMappers.ToUserPublicDto(u, storyFlags.Contains(u.Id))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        });
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("me/suggestions")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
        var storyFlags = await _stories.GetUserIdsWithActiveStoriesAsync(
            users.Select(u => u.Id),
            cancellationToken);

        return Ok(users.Select(u => EntityMappers.ToUserPublicDto(u, storyFlags.Contains(u.Id))).ToList());
    }

    [AllowAnonymous]
    [HttpGet("{userId}/communities")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<List<UserCommunityMembershipDto>>> GetUserCommunities(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var uid))
            return BadRequest();

        var rows = await _memberships.ListActiveWithCommunityAndTagsByUserAsync(uid, cancellationToken);
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        HashSet<int>? viewerMemberCommunityIds = null;
        if (viewerId.HasValue && viewerId.Value != uid)
        {
            var viewerCommunities = await _memberships.GetActiveCommunityIdsForUserAsync(viewerId.Value, cancellationToken);
            viewerMemberCommunityIds = viewerCommunities.ToHashSet();
        }

        var list = rows
            .OrderBy(m => m.Community.Name)
            .Select(m =>
            {
                var c = m.Community;
                var viewerSeesInterior = ViewerSeesPrivateCommunityInteriorOnProfile(
                    uid,
                    c,
                    viewerId,
                    viewerMemberCommunityIds);
                return new UserCommunityMembershipDto
                {
                    Community = EntityMappers.ToCommunityDto(c, viewerSeesInterior),
                    Role = m.Role
                };
            })
            .ToList();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        if (id == null)
            return Unauthorized();

        var profile = await BuildProfileAsync(id.Value, viewerId: id.Value, cancellationToken);
        return profile == null ? NotFound() : Ok(profile);
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPatch("me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

        string? error;
        if (!UsernameInputValidator.TryValidate(body.Username, out var username, out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeRequiredText(
                body.Name,
                "Nome",
                InputValidationLimits.DisplayNameMaxLength,
                out var displayName,
                out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeOptionalText(
                body.Bio,
                "Bio",
                InputValidationLimits.ProfileBioMaxLength,
                out var bio,
                out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeOptionalText(
                body.Pronouns,
                "Pronomes",
                InputValidationLimits.ProfilePronounsMaxLength,
                out var pronouns,
                out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeOptionalText(
                body.Location,
                "Localização",
                InputValidationLimits.ProfileLocationMaxLength,
                out var location,
                out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeOptionalText(
                body.Profession,
                "Título ou profissão",
                InputValidationLimits.ProfileProfessionMaxLength,
                out var profession,
                out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeHttpsImageUrl(body.AvatarUrl, out var avatarUrl, out error)
            || !InputValidator.TryNormalizeHttpsImageUrl(body.BannerUrl, out var bannerUrl, out error))
            return BadRequest(new { error });

        if (!TryNormalizeInterests(body.Interests, out var interests, out error))
            return BadRequest(new { error });

        if (!string.Equals(user.Username, username, StringComparison.Ordinal))
        {
            if (await _users.ExistsUsernameAsync(username))
                return Conflict(new { error = "Nome de utilizador já existe." });

            var now = DateTime.UtcNow;
            await _usernameHistory.AddAsync(new UsernameHistory
            {
                UserId = user.Id,
                OldUsername = user.Username,
                NewUsername = username,
                ChangedAt = now
            }, cancellationToken);

            user.Username = username;
        }

        user.DisplayName = displayName;
        user.Bio = bio ?? string.Empty;
        user.Pronouns = pronouns;
        user.Location = location;
        user.Profession = profession;
        user.ProfilePic = avatarUrl;
        user.BannerPic = bannerUrl;
        user.UpdatedAt = DateTime.UtcNow;

        if (body.Interests != null)
        {
            var existing = await _users.GetInterestsTrackedByUserIdAsync(user.Id, cancellationToken);
            _users.RemoveUserInterests(existing);
            foreach (var interest in interests)
            {
                _users.AddUserInterest(new UserInterest
                {
                    UserId = user.Id,
                    Label = interest
                });
            }
        }

        await _users.SaveChangesAsync();

        var profile = await BuildProfileAsync(user.Id, viewerId: id.Value, cancellationToken);
        return Ok(profile);
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPatch("me/interests")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

        if (!TryNormalizeInterests(body.Interests, out var interests, out var error))
            return BadRequest(new { error });

        var existing = await _users.GetInterestsTrackedByUserIdAsync(user.Id, cancellationToken);
        _users.RemoveUserInterests(existing);
        foreach (var interest in interests)
        {
            _users.AddUserInterest(new UserInterest
            {
                UserId = user.Id,
                Label = interest
            });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();

        var profile = await BuildProfileAsync(user.Id, viewerId: id.Value, cancellationToken);
        return Ok(profile);
    }

    [AllowAnonymous]
    [HttpGet("by-username/{username}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<UserProfileDto>> GetByUsername(
        string username,
        CancellationToken cancellationToken)
    {
        var resolution = await _usernameResolver.ResolveAsync(username, cancellationToken);
        if (resolution == null)
            return NotFound();

        var viewerId = User?.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var profile = await BuildProfileAsync(resolution.Value.UserId, viewerId, cancellationToken);
        if (profile == null)
            return NotFound();

        if (resolution.Value.ResolvedViaHistory)
            profile.CanonicalUsername = resolution.Value.CurrentUsername;

        return Ok(profile);
    }

    /// <summary>Legado — preferir <c>GET /users/by-username/{username}</c>.</summary>
    [HttpGet("{userId}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
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
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<ProfilePostsPageResponseDto>> GetUserPosts(
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

        var (pinned, posts, unpinnedTotal, allVisible) =
            await _posts.GetProfilePostsPageAsync(uid, viewerId, page, pageSize, cancellationToken);

        var pinnedDtos = await _postEnrichment.ToPostDtosAsync(pinned, viewerId, cancellationToken);
        var itemDtos = await _postEnrichment.ToPostDtosAsync(posts, viewerId, cancellationToken);

        return Ok(new ProfilePostsPageResponseDto
        {
            Pinned = pinnedDtos,
            Items = itemDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = allVisible,
            UnpinnedTotalCount = unpinnedTotal,
            HasNextPage = page * pageSize < unpinnedTotal,
            HasPreviousPage = page > 1
        });
    }

    [AllowAnonymous]
    [HttpGet("{userId}/follow/status")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<UserFollowStatusResponseDto>> GetFollowStatus(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var targetId))
            return BadRequest();

        if (await _users.GetByIdNoTrackingAsync(targetId, cancellationToken) == null)
            return NotFound();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        bool? isFollowing = null;
        if (viewerId.HasValue && viewerId.Value != targetId)
            isFollowing = await _follows.ExistsAsync(viewerId.Value, targetId, cancellationToken);

        var followersCount = await _follows.CountFollowersAsync(targetId, cancellationToken);
        var followingCount = await _follows.CountFollowingAsync(targetId, cancellationToken);

        return Ok(new UserFollowStatusResponseDto
        {
            TargetUserId = targetId.ToString(),
            IsFollowing = isFollowing,
            FollowersCount = followersCount,
            FollowingCount = followingCount
        });
    }

    [AllowAnonymous]
    [HttpGet("{userId}/followers")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<PaginatedResponseDto<UserPublicDto>>> GetFollowers(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var uid))
            return BadRequest();

        if (await _users.GetByIdNoTrackingAsync(uid, cancellationToken) == null)
            return NotFound();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _follows.ListFollowersPagedAsync(uid, page, pageSize, cancellationToken);
        var followerStoryFlags = await _stories.GetUserIdsWithActiveStoriesAsync(
            items.Select(u => u.Id),
            cancellationToken);

        return Ok(new PaginatedResponseDto<UserPublicDto>
        {
            Items = items.Select(u => EntityMappers.ToUserPublicDto(u, followerStoryFlags.Contains(u.Id))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        });
    }

    [AllowAnonymous]
    [HttpGet("{userId}/following")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<PaginatedResponseDto<UserPublicDto>>> GetUserFollowingList(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var uid))
            return BadRequest();

        if (await _users.GetByIdNoTrackingAsync(uid, cancellationToken) == null)
            return NotFound();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _follows.ListFollowingPagedAsync(uid, page, pageSize, cancellationToken);
        var followingStoryFlags = await _stories.GetUserIdsWithActiveStoriesAsync(
            items.Select(u => u.Id),
            cancellationToken);

        return Ok(new PaginatedResponseDto<UserPublicDto>
        {
            Items = items.Select(u => EntityMappers.ToUserPublicDto(u, followingStoryFlags.Contains(u.Id))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        });
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("{userId}/follow")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<FollowMutationResponseDto>> Follow(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var targetId))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (me.Value == targetId)
            return BadRequest(new { error = "Não podes seguir a ti própria." });

        if (await _users.GetByIdNoTrackingAsync(targetId, cancellationToken) == null)
            return NotFound();

        if (!await _follows.ExistsAsync(me.Value, targetId, cancellationToken))
        {
            _follows.Add(new Follow
            {
                FollowingUserId = me.Value,
                FollowedUserId = targetId,
                CreatedAt = DateTime.UtcNow
            });
            await _follows.SaveChangesAsync(cancellationToken);
            await _notificationService.NotifyNewFollowerAsync(me.Value, targetId, cancellationToken);
        }

        var followersCount = await _follows.CountFollowersAsync(targetId, cancellationToken);
        return Ok(new FollowMutationResponseDto { IsFollowing = true, FollowersCount = followersCount });
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("{userId}/follow")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<FollowMutationResponseDto>> Unfollow(string userId, CancellationToken cancellationToken)
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

        var followersCount = await _follows.CountFollowersAsync(targetId, cancellationToken);
        return Ok(new FollowMutationResponseDto { IsFollowing = false, FollowersCount = followersCount });
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

        var followersCount = await _follows.CountFollowersAsync(userId, cancellationToken);
        var followingCount = await _follows.CountFollowingAsync(userId, cancellationToken);

        var isOwnProfile = viewerId.HasValue && viewerId.Value == userId;
        var hasActiveStories = await _stories.HasActiveStoriesAsync(userId, cancellationToken);
        var profile = EntityMappers.ToUserProfile(u, following, links, interests, followersCount, followingCount,
            includePrivateFields: isOwnProfile,
            hasActiveStories: hasActiveStories);
        if (isOwnProfile)
            profile.Subscription = SubscriptionDtoMapper.ToStateDto(u.Subscription, DateTime.UtcNow);

        return profile;
    }

    private static bool TryNormalizeInterests(
        IEnumerable<InterestItemDto>? raw,
        out List<string> interests,
        out string? error)
    {
        interests = new List<string>();
        error = null;

        if (raw == null)
            return true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (string.IsNullOrWhiteSpace(item.Label))
                continue;

            var label = item.Label.Trim();
            if (label.Length > InputValidationLimits.ProfileInterestLabelMaxLength)
            {
                error = $"Cada interesse não pode exceder {InputValidationLimits.ProfileInterestLabelMaxLength} caracteres.";
                return false;
            }

            if (!seen.Add(label))
                continue;

            interests.Add(label);
            if (interests.Count > InputValidationLimits.ProfileInterestsMaxCount)
            {
                error = $"Máximo de {InputValidationLimits.ProfileInterestsMaxCount} interesses.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Perfil lista comunidades onde o dono do perfil é membro. Interior de comunidade privada só na API
    /// se o visitante for o próprio dono do perfil ou também membro activo dessa comunidade.
    /// </summary>
    private static bool ViewerSeesPrivateCommunityInteriorOnProfile(
        int profileUserId,
        Community c,
        int? viewerId,
        HashSet<int>? viewerMemberCommunityIds)
    {
        if (!string.Equals(c.Visibility, "private", StringComparison.OrdinalIgnoreCase))
            return true;

        if (viewerId == profileUserId)
            return true;

        return viewerMemberCommunityIds?.Contains(c.Id) == true;
    }
}
