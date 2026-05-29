using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Configuration;
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
using Woody.Application.Validation;

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
    private readonly IResourceAuthorizationService _authorization;
    private readonly INotificationService _notificationService;
    private readonly IUserRelationshipVisibilityService _visibility;

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
        ICommunityPostBoostService communityPostBoosts,
        IResourceAuthorizationService authorization,
        INotificationService notificationService,
        IUserRelationshipVisibilityService visibility)
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
        _authorization = authorization;
        _notificationService = notificationService;
        _visibility = visibility;
    }

    /// <summary>Cria comunidade: exige benefícios Pro (<see cref="IUserEntitlementService.CanCreateCommunityAsync"/>); ownership e moderação seguem a membership.</summary>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.ContentCreate)]
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
        InputValidator.TryNormalizeHttpsImageUrl(body.AvatarUrl, out var avatarUrl, out _);
        InputValidator.TryNormalizeHttpsImageUrl(body.CoverUrl, out var coverUrl, out _);
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
            AvatarUrl = avatarUrl,
            CoverUrl = coverUrl,
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
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<List<CommunityResponseDto>>> List(CancellationToken cancellationToken)
    {
        var list = await _communities.ListWithTagsOrderedByNameAsync(cancellationToken);
        // Descoberta: privadas aparecem, mas sem interior (regras / descrição longa) na listagem global.
        return Ok(list.Select(c => EntityMappers.ToCommunityDto(c, viewerSeesPrivateInterior: false)).ToList());
    }

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}</c>.</remarks>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
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

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var viewerSeesPrivateInterior = await ViewerSeesPrivateCommunityInteriorAsync(c, viewerId, cancellationToken);
        return Ok(EntityMappers.ToCommunityDto(c, viewerSeesPrivateInterior));
    }

    [AllowAnonymous]
    [HttpGet("by-slug/{slug}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
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

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var viewerSeesPrivateInterior = await ViewerSeesPrivateCommunityInteriorAsync(c, viewerId, cancellationToken);
        return Ok(EntityMappers.ToCommunityDto(c, viewerSeesPrivateInterior));
    }

    [AllowAnonymous]
    [HttpGet("by-slug/{slug}/posts")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<PaginatedResponseDto<PostResponseDto>>> CommunityPostsBySlug(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var cid = await ResolveCommunityIdBySlugOrNullAsync(slug, cancellationToken);
        if (cid == null)
            return NotFound();
        return await CommunityPosts(cid.Value, page, pageSize, cancellationToken);
    }

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}/posts</c>.</remarks>
    [AllowAnonymous]
    [HttpGet("{id:int}/posts")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
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

        var (posts, total) = await _posts.ListByCommunityIdPagedAsync(
            id,
            page,
            pageSize,
            await GetHiddenUserIdsExcludeAsync(viewerId, cancellationToken),
            cancellationToken);

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

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}/join-requests</c>.</remarks>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("{id:int}/join-requests")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}/join-requests/me</c>.</remarks>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("{communityId}/join-requests/me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> MyJoinRequest(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var community = await _communities.GetByIdWithTagsNoTrackingAsync(cid, cancellationToken);
        if (community == null)
            return NotFound();

        var active = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(me.Value, cid, cancellationToken);
        if (active != null)
        {
            return Ok(new
            {
                status = "member",
                requestId = (string?)null,
                requestedAt = (string?)null,
                reviewedAt = (string?)null,
                rejectionReason = (string?)null,
                canRequest = false
            });
        }

        var pending = await _joinRequests.GetPendingNoTrackingForUserAndCommunityAsync(cid, me.Value, cancellationToken);
        if (pending != null)
        {
            return Ok(new
            {
                status = "pending",
                requestId = pending.Id.ToString(),
                requestedAt = pending.RequestedAt.ToUniversalTime().ToString("o"),
                reviewedAt = (string?)null,
                rejectionReason = (string?)null,
                canRequest = false
            });
        }

        var latest = await _joinRequests.GetLatestNoTrackingForUserAndCommunityAsync(cid, me.Value, cancellationToken);
        if (latest == null)
        {
            return Ok(new
            {
                status = "none",
                requestId = (string?)null,
                requestedAt = (string?)null,
                reviewedAt = (string?)null,
                rejectionReason = (string?)null,
                canRequest = true
            });
        }

        return latest.Status switch
        {
            "rejected" => Ok(new
            {
                status = "rejected",
                requestId = latest.Id.ToString(),
                requestedAt = latest.RequestedAt.ToUniversalTime().ToString("o"),
                reviewedAt = latest.ReviewedAt?.ToUniversalTime().ToString("o"),
                rejectionReason = latest.RejectionReason,
                canRequest = true
            }),
            "cancelled" => Ok(new
            {
                status = "cancelled",
                requestId = latest.Id.ToString(),
                requestedAt = latest.RequestedAt.ToUniversalTime().ToString("o"),
                reviewedAt = latest.ReviewedAt?.ToUniversalTime().ToString("o"),
                rejectionReason = (string?)null,
                canRequest = true
            }),
            "approved" => Ok(new
            {
                status = "approved",
                requestId = latest.Id.ToString(),
                requestedAt = latest.RequestedAt.ToUniversalTime().ToString("o"),
                reviewedAt = latest.ReviewedAt?.ToUniversalTime().ToString("o"),
                rejectionReason = (string?)null,
                canRequest = false
            }),
            _ => Ok(new
            {
                status = "none",
                requestId = (string?)null,
                requestedAt = (string?)null,
                reviewedAt = (string?)null,
                rejectionReason = (string?)null,
                canRequest = true
            })
        };
    }

    /// <remarks>Legado: preferir <c>POST /api/communities/by-slug/{slug}/join-requests/me/cancel</c>.</remarks>
    /// <summary>A própria utilizadora cancela o pedido de entrada pendente.</summary>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("{communityId}/join-requests/me/cancel")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> CancelMyJoinRequest(string communityId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(communityId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!await _communities.ExistsNoTrackingAsync(cid, cancellationToken))
            return NotFound();

        var pending = await _joinRequests.GetPendingTrackedForUserAndCommunityAsync(cid, me.Value, cancellationToken);
        if (pending == null)
            return NoContent();

        var now = DateTime.UtcNow;
        pending.Status = "cancelled";
        pending.UpdatedAt = now;
        await _joinRequests.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}/members</c>.</remarks>
    [AllowAnonymous]
    [HttpGet("{communityId}/members")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
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
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var community = await _communities.GetByIdWithTagsNoTrackingAsync(cid, cancellationToken);
        if (community == null)
            return NotFound();
        if (!await _authorization.CanReadCommunityMembersAsync(community, viewerId, cancellationToken))
            return Forbid();

        var (rows, total) = await _memberships.ListActiveMembersPagedOrderedAsync(
            cid,
            page,
            pageSize,
            await GetHiddenUserIdsExcludeAsync(viewerId, cancellationToken),
            cancellationToken);

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

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}/members/me</c>.</remarks>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("{communityId}/members/me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

    /// <remarks>Legado: preferir <c>GET /api/communities/by-slug/{slug}/premium/analytics</c>.</remarks>
    /// <summary>Dashboard analytics (staff + plano premium da comunidade). Fonte de verdade: servidor.</summary>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("{communityId}/premium/analytics")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("{communityId}/posts/{postId}/boost")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("{communityId}/posts/{postId}/boost")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("{communityId}/post-boosts")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

    /// <remarks>Legado: preferir <c>PATCH /api/communities/by-slug/{slug}</c>.</remarks>
    [Authorize(Policy = "VerifiedAccount")]
    [HttpPatch("{communityId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

        var validationError = CreateCommunityRequestValidator.ValidatePatch(body);
        if (validationError != null)
            return BadRequest(new { error = validationError });

        var c = await _communities.GetByIdTrackedWithTagsAsync(cid, cancellationToken);
        if (c == null)
            return NotFound();

        InputValidator.TryNormalizeHttpsImageUrl(body.AvatarUrl, out var avatarUrl, out _);
        InputValidator.TryNormalizeHttpsImageUrl(body.CoverUrl, out var coverUrl, out _);

        if (body.Name != null)
            c.Name = body.Name.Trim();
        if (body.Description != null)
            c.Description = body.Description.Trim();
        if (body.Category != null)
            c.Category = body.Category.Trim().ToLowerInvariant();
        if (body.Rules != null)
            c.Rules = body.Rules.Trim();
        if (body.Visibility != null)
            c.Visibility = body.Visibility.Trim().ToLowerInvariant();
        if (body.AvatarUrl != null)
            c.AvatarUrl = avatarUrl;
        if (body.CoverUrl != null)
            c.CoverUrl = coverUrl;

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

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("{communityId}/members")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

        var existingMember = await _memberships.GetForUserAndCommunityAsync(me.Value, cid, cancellationToken);
        if (MembershipIsBanned(existingMember))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "membership_banned",
                error = "Estás restrita nesta comunidade e não podes voltar a entrar por este fluxo."
            });
        }

        await EnsureMembershipAsync(me.Value, c, active: true, role: "member", cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("{communityId}/join-requests")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

        var existingMember = await _memberships.GetForUserAndCommunityAsync(me.Value, cid, cancellationToken);
        if (MembershipIsBanned(existingMember))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "membership_banned",
                error = "Estás restrita nesta comunidade e não podes solicitar entrada."
            });
        }

        if (string.Equals(c.Visibility, "public", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureMembershipAsync(me.Value, c, active: true, role: "member", cancellationToken);
            return NoContent();
        }

        if (existingMember is { Status: "active" })
            return NoContent();

        if (await _joinRequests.ExistsPendingAsync(cid, me.Value, cancellationToken))
            return NoContent();

        var now = DateTime.UtcNow;
        var jr = new JoinRequest
        {
            CommunityId = cid,
            UserId = me.Value,
            Status = "pending",
            RequestedAt = now,
            UpdatedAt = now
        };
        _joinRequests.Add(jr);
        try
        {
            await _joinRequests.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await _joinRequests.ExistsPendingAsync(cid, me.Value, cancellationToken))
                return NoContent();
            throw;
        }

        var mods = await _memberships.ListActiveModeratorUserIdsForCommunityAsync(cid, cancellationToken);
        await _notificationService.NotifyCommunityJoinRequestAsync(
            me.Value,
            cid,
            c.Slug,
            jr.Id,
            mods,
            cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("{communityId}/join")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> JoinAlias(string communityId, CancellationToken cancellationToken) =>
        RequestJoin(communityId, cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("{communityId}/members/me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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

    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("{communityId}/members/{userId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
        if (string.Equals(m.Role, "owner", StringComparison.OrdinalIgnoreCase))
            return Forbid();

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

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPatch("{communityId}/members/{userId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
        if (string.Equals(m.Role, "owner", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (!InputValidator.TryNormalizeOptionalText(
                body.Status,
                "Status",
                InputValidationLimits.MembershipStatusMaxLength,
                out var normalizedStatus,
                out var error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeOptionalText(
                body.Role,
                "Role",
                InputValidationLimits.MembershipRoleMaxLength,
                out var normalizedRole,
                out error))
            return BadRequest(new { error });

        var wasActive = string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase);
        if (normalizedStatus != null)
        {
            var status = normalizedStatus.ToLowerInvariant();
            if (status is not ("active" or "pending" or "banned"))
                return BadRequest(new { error = "Status inválido." });
            m.Status = status;
        }
        if (normalizedRole != null)
        {
            var role = normalizedRole.ToLowerInvariant();
            if (role is not ("admin" or "member"))
                return BadRequest(new { error = "Role inválido." });
            m.Role = role;
        }

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

    // --- Rotas por slug (preferidas em APIs públicas) ---

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("by-slug/{slug}/join-requests")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> PendingJoinRequestsBySlug(string slug, CancellationToken cancellationToken)
    {
        var cid = await ResolveCommunityIdBySlugOrNullAsync(slug, cancellationToken);
        if (cid == null)
            return NotFound();
        return await PendingJoinRequests(cid.Value, cancellationToken);
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("by-slug/{slug}/join-requests/me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> MyJoinRequestBySlug(string slug, CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(slug, id => MyJoinRequest(id.ToString(), cancellationToken), cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("by-slug/{slug}/join-requests/me/cancel")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> CancelMyJoinRequestBySlug(string slug, CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(slug, id => CancelMyJoinRequest(id.ToString(), cancellationToken), cancellationToken);

    [AllowAnonymous]
    [HttpGet("by-slug/{slug}/members")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public Task<ActionResult<PaginatedResponseDto<CommunityMemberItemDto>>> MembersBySlug(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ResolveSlugAndRunAsync(
            slug,
            id => Members(id.ToString(), page, pageSize, cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("by-slug/{slug}/members/me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> MyMembershipBySlug(string slug, CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(slug, id => MyMembership(id.ToString(), cancellationToken), cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("by-slug/{slug}/premium/analytics")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<ActionResult<CommunityPremiumDashboardAnalyticsDto>> CommunityPremiumAnalyticsBySlug(
        string slug,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default) =>
        ResolveSlugAndRunAsync(
            slug,
            id => CommunityPremiumAnalytics(id.ToString(), days, cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet("by-slug/{slug}/post-boosts")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<ActionResult<IReadOnlyList<CommunityPostBoostListItemDto>>> ListCommunityPostBoostsBySlug(
        string slug,
        CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(
            slug,
            id => ListCommunityPostBoosts(id.ToString(), cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("by-slug/{slug}/posts/{postId}/boost")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<ActionResult<CommunityPostBoostResponseDto>> BoostCommunityPostBySlug(
        string slug,
        string postId,
        [FromBody] CommunityPostBoostActivateRequestDto? body,
        CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(
            slug,
            id => BoostCommunityPost(id.ToString(), postId, body, cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("by-slug/{slug}/posts/{postId}/boost")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> UnboostCommunityPostBySlug(
        string slug,
        string postId,
        CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(
            slug,
            id => UnboostCommunityPost(id.ToString(), postId, cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPatch("by-slug/{slug}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<ActionResult<CommunityResponseDto>> PatchBySlug(
        string slug,
        [FromBody] CommunityUpdateRequestDTO body,
        CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(
            slug,
            id => Patch(id.ToString(), body, cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("by-slug/{slug}/members")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> JoinPublicBySlug(string slug, CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(slug, id => JoinPublic(id.ToString(), cancellationToken), cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPost("by-slug/{slug}/join-requests")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> RequestJoinBySlug(string slug, CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(slug, id => RequestJoin(id.ToString(), cancellationToken), cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("by-slug/{slug}/members/me")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> LeaveBySlug(string slug, CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(slug, id => Leave(id.ToString(), cancellationToken), cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpDelete("by-slug/{slug}/members/{userId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> RemoveMemberBySlug(
        string slug,
        string userId,
        CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(
            slug,
            id => RemoveMember(id.ToString(), userId, cancellationToken),
            cancellationToken);

    [Authorize(Policy = "VerifiedAccount")]
    [HttpPatch("by-slug/{slug}/members/{userId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public Task<IActionResult> PatchMemberBySlug(
        string slug,
        string userId,
        [FromBody] MembershipPatchRequestDTO body,
        CancellationToken cancellationToken) =>
        ResolveSlugAndRunAsync(
            slug,
            id => PatchMember(id.ToString(), userId, body, cancellationToken),
            cancellationToken);

    private async Task<int?> ResolveCommunityIdBySlugOrNullAsync(string slug, CancellationToken cancellationToken)
    {
        var c = await _communities.GetBySlugWithTagsNoTrackingAsync(slug, cancellationToken);
        return c?.Id;
    }

    private async Task<IActionResult> ResolveSlugAndRunAsync(
        string slug,
        Func<int, Task<IActionResult>> action,
        CancellationToken cancellationToken)
    {
        var cid = await ResolveCommunityIdBySlugOrNullAsync(slug, cancellationToken);
        if (cid == null)
            return NotFound();
        return await action(cid.Value);
    }

    private async Task<ActionResult<T>> ResolveSlugAndRunAsync<T>(
        string slug,
        Func<int, Task<ActionResult<T>>> action,
        CancellationToken cancellationToken)
    {
        var cid = await ResolveCommunityIdBySlugOrNullAsync(slug, cancellationToken);
        if (cid == null)
            return NotFound();
        return await action(cid.Value);
    }

    private async Task<bool> ViewerSeesPrivateCommunityInteriorAsync(
        Community c,
        int? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(c.Visibility, "private", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!viewerUserId.HasValue)
            return false;

        var member = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(
            viewerUserId.Value,
            c.Id,
            cancellationToken);
        return member != null;
    }

    private async Task<IReadOnlyCollection<int>?> GetHiddenUserIdsExcludeAsync(
        int? viewerUserId,
        CancellationToken cancellationToken)
    {
        if (!viewerUserId.HasValue)
            return null;

        var hiddenIds = await _visibility.GetHiddenUserIdsForViewerAsync(viewerUserId.Value, cancellationToken);
        return hiddenIds.Count > 0 ? hiddenIds : null;
    }

    private static bool MembershipIsBanned(CommunityMembership? m) =>
        m != null && string.Equals(m.Status, "banned", StringComparison.OrdinalIgnoreCase);

    private async Task EnsureMembershipAsync(int userId, Community c, bool active, string role, CancellationToken cancellationToken)
    {
        var existing = await _memberships.GetForUserAndCommunityAsync(userId, c.Id, cancellationToken);
        if (existing != null)
        {
            if (active)
            {
                if (MembershipIsBanned(existing))
                {
                    throw new InvalidOperationException(
                        "Membership banned cannot be reactivated via EnsureMembershipAsync.");
                }

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
