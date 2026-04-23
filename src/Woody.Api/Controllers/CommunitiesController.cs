using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Billing;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Application.Mapping;
using Woody.Application.Services;
using Woody.Application.Utilities;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/communities")]
public class CommunitiesController : ControllerBase
{
    private readonly ICommunityRepository _communities;
    private readonly ICommunityMembershipRepository _memberships;
    private readonly IJoinRequestRepository _joinRequests;
    private readonly IPostRepository _posts;
    private readonly IPostEnrichmentService _postEnrichment;
    private readonly ICommunityPermissionService _permission;
    private readonly IUserEntitlementService _entitlements;
    private readonly ICommunitySubscriptionRepository _communitySubscriptions;
    private readonly ICommunityPremiumEntitlementService _communityPremiumEntitlements;
    private readonly ICommunityDailyRollupRepository _dailyRollups;
    private readonly ICommunityDashboardAnalyticsService _communityDashboardAnalytics;
    private readonly ICommunityPostBoostService _communityPostBoosts;

    public CommunitiesController(
        ICommunityRepository communities,
        ICommunityMembershipRepository memberships,
        IJoinRequestRepository joinRequests,
        IPostRepository posts,
        IPostEnrichmentService postEnrichment,
        ICommunityPermissionService permission,
        IUserEntitlementService entitlements,
        ICommunitySubscriptionRepository communitySubscriptions,
        ICommunityPremiumEntitlementService communityPremiumEntitlements,
        ICommunityDailyRollupRepository dailyRollups,
        ICommunityDashboardAnalyticsService communityDashboardAnalytics,
        ICommunityPostBoostService communityPostBoosts)
    {
        _communities = communities;
        _memberships = memberships;
        _joinRequests = joinRequests;
        _posts = posts;
        _postEnrichment = postEnrichment;
        _permission = permission;
        _entitlements = entitlements;
        _communitySubscriptions = communitySubscriptions;
        _communityPremiumEntitlements = communityPremiumEntitlements;
        _dailyRollups = dailyRollups;
        _communityDashboardAnalytics = communityDashboardAnalytics;
        _communityPostBoosts = communityPostBoosts;
    }

    /// <summary>Cria comunidade: exige benefícios Pro (<see cref="IUserEntitlementService.CanCreateCommunityAsync"/>); ownership e moderação seguem a membership.</summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CommunityResponseDto>> Create(
        [FromBody] CreateCommunityRequestDTO body,
        CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await _entitlements.CanCreateCommunityAsync(me.Value, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "pro_required",
                error = "Criar comunidades é uma funcionalidade Woody Pro."
            });
        }

        var validationError = CreateCommunityRequestValidator.Validate(body);
        if (validationError != null)
            return BadRequest(new { error = validationError });

        var tags = CreateCommunityRequestValidator.NormalizeTags(body.Tags);
        var baseSlug = CommunitySlugHelper.SlugifyBase(body.Name);
        var slug = baseSlug;
        var suffix = 2;
        while (await _communities.ExistsSlugNoTrackingAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        var visibility = string.Equals(body.Visibility?.Trim(), "private", StringComparison.OrdinalIgnoreCase)
            ? "private"
            : "public";
        var now = DateTime.UtcNow;

        var community = new Community
        {
            Slug = slug,
            Name = body.Name.Trim(),
            Description = body.Description.Trim(),
            Category = body.Category.Trim().ToLowerInvariant(),
            Rules = string.IsNullOrWhiteSpace(body.Rules) ? string.Empty : body.Rules.Trim(),
            Visibility = visibility,
            OwnerUserId = me.Value,
            AvatarUrl = string.IsNullOrWhiteSpace(body.AvatarUrl) ? null : body.AvatarUrl.Trim(),
            CoverUrl = string.IsNullOrWhiteSpace(body.CoverUrl) ? null : body.CoverUrl.Trim(),
            MemberCount = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        _communities.Add(community);
        await _communities.SaveChangesAsync(cancellationToken);

        await _communitySubscriptions.AddAsync(new CommunitySubscription
        {
            CommunityId = community.Id,
            Plan = CommunityPlan.Free,
            Status = SubscriptionStatus.Active,
            PlanCode = CommunityBillingPlanCodes.Free,
            BillingProvider = BillingProvider.None,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
        await _communitySubscriptions.SaveChangesAsync(cancellationToken);

        foreach (var tag in tags)
            _communities.AddCommunityTag(new CommunityTag { CommunityId = community.Id, Tag = tag });

        _memberships.Add(new CommunityMembership
        {
            UserId = me.Value,
            CommunityId = community.Id,
            Role = "owner",
            Status = "active",
            JoinedAt = now
        });

        await _memberships.SaveChangesAsync(cancellationToken);

        var created = await _communities.GetByIdWithTagsNoTrackingAsync(community.Id, cancellationToken)
            ?? throw new InvalidOperationException("Comunidade criada mas não encontrada.");
        return Ok(EntityMappers.ToCommunityDto(created));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<CommunityResponseDto>>> List(CancellationToken cancellationToken)
    {
        var list = await _communities.ListWithTagsOrderedByNameAsync(cancellationToken);
        return Ok(list.Select(EntityMappers.ToCommunityDto).ToList());
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommunityResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var c = await _communities.GetByIdWithTagsNoTrackingAsync(id, cancellationToken);
        if (c == null)
            return NotFound();
        try
        {
            await _dailyRollups.IncrementPageViewAsync(id, DateTime.UtcNow, cancellationToken);
        }
        catch
        {
            // Métricas não devem impedir o carregamento público da comunidade.
        }

        return Ok(EntityMappers.ToCommunityDto(c));
    }

    [AllowAnonymous]
    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<CommunityResponseDto>> BySlug(string slug, CancellationToken cancellationToken)
    {
        var c = await _communities.GetBySlugWithTagsNoTrackingAsync(slug, cancellationToken);
        if (c == null)
            return NotFound();
        try
        {
            await _dailyRollups.IncrementPageViewAsync(c.Id, DateTime.UtcNow, cancellationToken);
        }
        catch
        {
            // Métricas não devem impedir o carregamento público da comunidade.
        }

        return Ok(EntityMappers.ToCommunityDto(c));
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

        var c = await _communities.GetByIdWithTagsNoTrackingAsync(id, cancellationToken);
        if (c == null)
            return NotFound();

        if (!string.Equals(c.Visibility, "public", StringComparison.OrdinalIgnoreCase))
        {
            if (viewerId == null)
                return Forbid();
            var member = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(viewerId.Value, id, cancellationToken);
            if (member == null)
                return Forbid();
        }

        var (posts, total) = await _posts.ListByCommunityIdPagedAsync(id, page, pageSize, cancellationToken);

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
    [HttpGet("{id:int}/join-requests")]
    public async Task<IActionResult> PendingJoinRequests(int id, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await _permission.CanModerateCommunityAsync(id, me.Value, cancellationToken))
            return Forbid();

        var rows = await _joinRequests.ListPendingWithUserForCommunityAsync(id, cancellationToken);

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
    public async Task<ActionResult<PaginatedResponseDto<CommunityMemberItemDto>>> Members(
        string communityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (rows, total) = await _memberships.ListActiveMembersPagedOrderedAsync(cid, page, pageSize, cancellationToken);

        return Ok(new PaginatedResponseDto<CommunityMemberItemDto>
        {
            Items = rows.Select(m => new CommunityMemberItemDto
            {
                User = EntityMappers.ToUserPublicDto(m.User),
                Role = m.Role
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasNextPage = page * pageSize < total,
            HasPreviousPage = page > 1
        });
    }

    [Authorize]
    [HttpGet("{communityId}/members/me")]
    public async Task<IActionResult> MyMembership(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var row = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(me.Value, cid, cancellationToken);

        if (row == null)
            return Ok(new { isMember = false, role = (string?)null, premiumCapabilities = (CommunityPremiumCapabilitiesDto?)null });

        var caps = await _communityPremiumEntitlements.GetCapabilitiesAsync(cid, me.Value, cancellationToken);
        return Ok(new { isMember = true, role = row.Role, premiumCapabilities = caps });
    }

    /// <summary>Dashboard analytics (staff + plano premium da comunidade). Fonte de verdade: servidor.</summary>
    [Authorize]
    [HttpGet("{communityId}/premium/analytics")]
    public async Task<ActionResult<CommunityPremiumDashboardAnalyticsDto>> CommunityPremiumAnalytics(
        string communityId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var caps = await _communityPremiumEntitlements.GetCapabilitiesAsync(cid, me.Value, cancellationToken);
        if (!caps.IsStaffForPremiumTools)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "community_staff_required",
                error = "Só owner ou admin pode consultar analytics desta comunidade."
            });
        }

        if (!caps.CommunityPremiumActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "community_premium_required",
                error = "Plano premium ativo da comunidade necessário para analytics."
            });
        }

        var c = await _communities.GetByIdWithTagsNoTrackingAsync(cid, cancellationToken);
        if (c == null)
            return NotFound();

        var dto = await _communityDashboardAnalytics.BuildDashboardAsync(cid, c.Slug, days, cancellationToken);
        return Ok(dto);
    }

    /// <summary>Activa impulsionamento (owner/admin + premium da comunidade).</summary>
    [Authorize]
    [HttpPost("{communityId}/posts/{postId}/boost")]
    public async Task<ActionResult<CommunityPostBoostResponseDto>> BoostCommunityPost(
        string communityId,
        string postId,
        [FromBody] CommunityPostBoostActivateRequestDto? body,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid) || !int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var (dto, err) = await _communityPostBoosts.ActivateAsync(cid, pid, me.Value, body?.DurationDays, cancellationToken);
        return err switch
        {
            "community_staff_required" => StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = err,
                error = "Só owner ou admin pode impulsionar posts desta comunidade."
            }),
            "community_premium_required" => StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = err,
                error = "Plano premium ativo da comunidade necessário para impulsionar publicações."
            }),
            "post_not_found" => NotFound(),
            "post_not_in_community" => BadRequest(new { error = "Esta publicação não pertence a esta comunidade." }),
            _ => dto == null
                ? StatusCode(StatusCodes.Status500InternalServerError)
                : StatusCode(StatusCodes.Status201Created, dto)
        };
    }

    /// <summary>Cancela impulsionamento activo do post nesta comunidade.</summary>
    [Authorize]
    [HttpDelete("{communityId}/posts/{postId}/boost")]
    public async Task<IActionResult> UnboostCommunityPost(
        string communityId,
        string postId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid) || !int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var (ok, err) = await _communityPostBoosts.DeactivateAsync(cid, pid, me.Value, cancellationToken);
        if (!ok)
        {
            return err switch
            {
                "community_staff_required" => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = err,
                    error = "Só owner ou admin pode gerir impulsionamentos."
                }),
                "community_premium_required" => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = err,
                    error = "Plano premium ativo da comunidade necessário."
                }),
                "post_not_found" => NotFound(),
                "post_not_in_community" => BadRequest(new { error = "Esta publicação não pertence a esta comunidade." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        return NoContent();
    }

    /// <summary>Lista impulsionamentos activos (staff + premium).</summary>
    [Authorize]
    [HttpGet("{communityId}/post-boosts")]
    public async Task<ActionResult<IReadOnlyList<CommunityPostBoostListItemDto>>> ListCommunityPostBoosts(
        string communityId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var (items, err) = await _communityPostBoosts.ListActiveAsync(cid, me.Value, cancellationToken);
        return err switch
        {
            "community_staff_required" => StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = err,
                error = "Só owner ou admin pode listar impulsionamentos."
            }),
            "community_premium_required" => StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = err,
                error = "Plano premium ativo da comunidade necessário."
            }),
            _ => Ok(items)
        };
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

        if (!await _permission.CanModerateCommunityAsync(cid, me.Value, cancellationToken))
            return Forbid();

        var c = await _communities.GetByIdTrackedWithTagsAsync(cid, cancellationToken);
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
            _communities.RemoveCommunityTags(c.Tags);
            foreach (var t in body.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
                _communities.AddCommunityTag(new CommunityTag { CommunityId = c.Id, Tag = t.Trim() });
        }

        c.UpdatedAt = DateTime.UtcNow;
        await _communities.SaveChangesAsync(cancellationToken);

        c = await _communities.GetByIdWithTagsNoTrackingAsync(cid, cancellationToken)
            ?? throw new InvalidOperationException("Community not found after update.");
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

        var c = await _communities.GetByIdTrackedAsync(cid, cancellationToken);
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

        var c = await _communities.GetByIdTrackedAsync(cid, cancellationToken);
        if (c == null)
            return NotFound();

        if (string.Equals(c.Visibility, "public", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureMembershipAsync(me.Value, c, active: true, role: "member", cancellationToken);
            return NoContent();
        }

        var existingMember = await _memberships.GetForUserAndCommunityAsync(me.Value, cid, cancellationToken);
        if (existingMember is { Status: "active" })
            return NoContent();

        if (await _joinRequests.ExistsPendingAsync(cid, me.Value, cancellationToken))
            return NoContent();

        _joinRequests.Add(new JoinRequest
        {
            CommunityId = cid,
            UserId = me.Value,
            Status = "pending",
            RequestedAt = DateTime.UtcNow
        });
        await _joinRequests.SaveChangesAsync(cancellationToken);
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

        var m = await _memberships.GetForUserAndCommunityAsync(me.Value, cid, cancellationToken);
        if (m == null)
            return NoContent();

        if (m.Role == "owner")
            return BadRequest();

        var wasActive = string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase);
        _memberships.Remove(m);
        await _memberships.SaveChangesAsync(cancellationToken);
        if (wasActive)
            await _dailyRollups.IncrementMemberLeaveAsync(cid, DateTime.UtcNow, cancellationToken);

        var c = await _communities.GetByIdTrackedAsync(cid, cancellationToken)
                ?? throw new InvalidOperationException("Community not found.");
        c.MemberCount = await _memberships.CountActiveInCommunityAsync(cid, cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;
        await _communities.SaveChangesAsync(cancellationToken);
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

        if (!await _permission.CanModerateCommunityAsync(cid, me.Value, cancellationToken))
            return Forbid();

        var m = await _memberships.GetForUserAndCommunityAsync(uid, cid, cancellationToken);
        if (m == null)
            return NoContent();

        var wasActive = string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase);
        _memberships.Remove(m);
        await _memberships.SaveChangesAsync(cancellationToken);

        var c = await _communities.GetByIdTrackedAsync(cid, cancellationToken)
                ?? throw new InvalidOperationException("Community not found.");
        c.MemberCount = await _memberships.CountActiveInCommunityAsync(cid, cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;
        await _communities.SaveChangesAsync(cancellationToken);

        if (wasActive)
            await _dailyRollups.IncrementMemberLeaveAsync(cid, DateTime.UtcNow, cancellationToken);

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

        if (!await _permission.CanModerateCommunityAsync(cid, me.Value, cancellationToken))
            return Forbid();

        var m = await _memberships.GetForUserAndCommunityAsync(uid, cid, cancellationToken);
        if (m == null)
            return NotFound();

        var wasActive = string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(body.Status))
            m.Status = body.Status.Trim();
        if (!string.IsNullOrWhiteSpace(body.Role))
            m.Role = body.Role.Trim();

        var nowActive = string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase);
        await _memberships.SaveChangesAsync(cancellationToken);
        if (wasActive && !nowActive)
            await _dailyRollups.IncrementMemberLeaveAsync(cid, DateTime.UtcNow, cancellationToken);

        var comm = await _communities.GetByIdTrackedAsync(cid, cancellationToken)
                   ?? throw new InvalidOperationException("Community not found.");
        comm.MemberCount = await _memberships.CountActiveInCommunityAsync(cid, cancellationToken);
        comm.UpdatedAt = DateTime.UtcNow;
        await _communities.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task EnsureMembershipAsync(int userId, Community c, bool active, string role, CancellationToken cancellationToken)
    {
        var existing = await _memberships.GetForUserAndCommunityAsync(userId, c.Id, cancellationToken);
        if (existing != null)
        {
            if (active)
            {
                existing.Status = "active";
                existing.Role = existing.Role == "owner" ? "owner" : role;
                existing.JoinedAt ??= DateTime.UtcNow;
            }

            await _memberships.SaveChangesAsync(cancellationToken);
            c.MemberCount = await _memberships.CountActiveInCommunityAsync(c.Id, cancellationToken);
            c.UpdatedAt = DateTime.UtcNow;
            await _communities.SaveChangesAsync(cancellationToken);
            return;
        }

        _memberships.Add(new CommunityMembership
        {
            UserId = userId,
            CommunityId = c.Id,
            Role = role,
            Status = active ? "active" : "pending",
            JoinedAt = active ? DateTime.UtcNow : null
        });
        await _memberships.SaveChangesAsync(cancellationToken);
        c.MemberCount = await _memberships.CountActiveInCommunityAsync(c.Id, cancellationToken);
        c.UpdatedAt = DateTime.UtcNow;
        await _communities.SaveChangesAsync(cancellationToken);
    }
}
