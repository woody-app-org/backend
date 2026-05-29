using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class CommunitiesBySlugControllerTests
{
    [Fact]
    public async Task CommunityPostsBySlug_ReturnsPosts_ForPublicCommunity()
    {
        var community = SampleCommunity(5, "club-woody", "public");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.ListByCommunityIdPagedAsync(5, 1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Post>(), 0));

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment.Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostResponseDto>());

        var controller = CreateController(community, posts: posts, enrichment: enrichment);

        var result = await controller.CommunityPostsBySlug("club-woody", cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CommunityPostsBySlug_ReturnsForbid_ForPrivateCommunityWithoutMember()
    {
        var community = SampleCommunity(6, "secret-club", "private");
        var controller = CreateController(community, viewerUserId: null);

        var result = await controller.CommunityPostsBySlug("secret-club", cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CommunityPostsBySlug_ReturnsNotFound_WhenSlugUnknown()
    {
        var communities = new Mock<ICommunityRepository>();
        communities.Setup(x => x.GetBySlugWithTagsNoTrackingAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Community?)null);

        var controller = CreateController(null, communities: communities);

        var result = await controller.CommunityPostsBySlug("missing", cancellationToken: CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task MembersBySlug_BlocksPrivateCommunity_ForAnonymous()
    {
        var community = SampleCommunity(10, "private-club", "private");
        var controller = CreateController(community, canReadMembers: false, viewerUserId: null);

        var result = await controller.MembersBySlug("private-club", cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PatchBySlug_ReturnsForbid_WhenViewerIsNotModerator()
    {
        var community = SampleCommunity(7, "edit-me", "public");
        var permission = new Mock<ICommunityPermissionService>();
        permission.Setup(x => x.CanModerateCommunityAsync(7, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = CreateController(community, permission: permission, viewerUserId: 20);

        var result = await controller.PatchBySlug(
            "edit-me",
            new CommunityUpdateRequestDTO { Name = "Novo nome" },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PatchBySlug_ReturnsOk_WhenViewerIsAdmin()
    {
        var community = SampleCommunity(8, "admin-club", "public");
        var tracked = SampleCommunity(8, "admin-club", "public");
        var communities = new Mock<ICommunityRepository>();
        communities.Setup(x => x.GetBySlugWithTagsNoTrackingAsync("admin-club", It.IsAny<CancellationToken>()))
            .ReturnsAsync(community);
        communities.Setup(x => x.GetByIdWithTagsNoTrackingAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(community);
        communities.Setup(x => x.GetByIdTrackedWithTagsAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);
        communities.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var permission = new Mock<ICommunityPermissionService>();
        permission.Setup(x => x.CanModerateCommunityAsync(8, 30, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = CreateController(community, communities: communities, permission: permission, viewerUserId: 30);

        var result = await controller.PatchBySlug(
            "admin-club",
            new CommunityUpdateRequestDTO { Name = "Atualizado" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<CommunityResponseDto>(ok.Value);
    }

    [Fact]
    public async Task RequestJoinBySlug_ReturnsNoContent_ForPrivateCommunity()
    {
        var community = SampleCommunity(9, "join-me", "private");
        var communities = new Mock<ICommunityRepository>();
        communities.Setup(x => x.GetBySlugWithTagsNoTrackingAsync("join-me", It.IsAny<CancellationToken>()))
            .ReturnsAsync(community);
        communities.Setup(x => x.GetByIdTrackedAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(community);
        communities.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var memberships = new Mock<ICommunityMembershipRepository>();
        memberships.Setup(x => x.GetForUserAndCommunityAsync(40, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommunityMembership?)null);

        var joinRequests = new Mock<IJoinRequestRepository>();
        joinRequests.Setup(x => x.ExistsPendingAsync(9, 40, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        joinRequests.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        joinRequests.Setup(x => x.Add(It.IsAny<JoinRequest>()));

        var membershipsForMods = new Mock<ICommunityMembershipRepository>();
        membershipsForMods.Setup(x => x.GetForUserAndCommunityAsync(40, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommunityMembership?)null);
        membershipsForMods.Setup(x => x.ListActiveModeratorUserIdsForCommunityAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });

        var controller = CreateController(
            community,
            communities: communities,
            memberships: membershipsForMods,
            joinRequests: joinRequests,
            viewerUserId: 40);

        var result = await controller.RequestJoinBySlug("join-me", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // -------------------------------------------------------------------------
    // IDOR: admin de comunidade A não pode gerir comunidade B
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PatchBySlug_CommunityAdminA_CannotModerate_CommunityB()
    {
        // Admin do utilizador 99 tem permissão apenas na comunidade 1 (slug-a).
        // Tenta fazer patch na comunidade 2 (slug-b) → deve retornar Forbid.
        var communityB = SampleCommunity(2, "slug-b", "public");
        var permission = new Mock<ICommunityPermissionService>();
        permission
            .Setup(x => x.CanModerateCommunityAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);   // A
        permission
            .Setup(x => x.CanModerateCommunityAsync(2, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);  // B

        var controller = CreateController(communityB, permission: permission, viewerUserId: 99);

        var result = await controller.PatchBySlug(
            "slug-b",
            new CommunityUpdateRequestDTO { Name = "Hacked" },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task MembersBySlug_CommunityAdminA_CannotListMembers_PrivateCommunityB()
    {
        // Não-membro da comunidade B não consegue ver os membros (privada).
        var communityB = SampleCommunity(3, "secret-b", "private");
        var controller = CreateController(communityB, canReadMembers: false, viewerUserId: 99);

        var result = await controller.MembersBySlug("secret-b", cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CommunityPosts_LegacyById_StillWorks()
    {
        var community = SampleCommunity(15, "legacy-club", "public");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.ListByCommunityIdPagedAsync(15, 1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Post>(), 0));

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment.Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostResponseDto>());

        var controller = CreateController(community, posts: posts, enrichment: enrichment);

        var result = await controller.CommunityPosts(15, cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static Community SampleCommunity(int id, string slug, string visibility) => new()
    {
        Id = id,
        Slug = slug,
        Name = slug,
        Description = "Desc",
        Category = "general",
        Rules = string.Empty,
        Visibility = visibility,
        OwnerUserId = 1,
        MemberCount = 1
    };

    private static CommunitiesController CreateController(
        Community? community,
        Mock<ICommunityRepository>? communities = null,
        Mock<ICommunityMembershipRepository>? memberships = null,
        Mock<IJoinRequestRepository>? joinRequests = null,
        Mock<IPostRepository>? posts = null,
        Mock<IPostEnrichmentService>? enrichment = null,
        Mock<ICommunityPermissionService>? permission = null,
        Mock<IResourceAuthorizationService>? authorization = null,
        int? viewerUserId = null,
        bool canReadMembers = true)
    {
        communities ??= new Mock<ICommunityRepository>();
        if (community != null)
        {
            communities.Setup(x => x.GetBySlugWithTagsNoTrackingAsync(community.Slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync(community);
            communities.Setup(x => x.GetByIdWithTagsNoTrackingAsync(community.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(community);
        }

        memberships ??= new Mock<ICommunityMembershipRepository>();
        if (community != null && viewerUserId.HasValue)
        {
            memberships.Setup(x => x.GetActiveForUserAndCommunityNoTrackingAsync(viewerUserId.Value, community.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CommunityMembership?)null);
        }

        authorization ??= new Mock<IResourceAuthorizationService>();
        if (community != null)
        {
            authorization.Setup(x => x.CanReadCommunityMembersAsync(community, viewerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(canReadMembers);
        }

        var controller = new CommunitiesController(
            communities.Object,
            memberships.Object,
            joinRequests?.Object ?? Mock.Of<IJoinRequestRepository>(),
            posts?.Object ?? Mock.Of<IPostRepository>(),
            enrichment?.Object ?? Mock.Of<IPostEnrichmentService>(),
            permission?.Object ?? Mock.Of<ICommunityPermissionService>(),
            Mock.Of<IUserEntitlementService>(),
            Mock.Of<ICommunitySubscriptionRepository>(),
            Mock.Of<ICommunityPremiumEntitlementService>(),
            Mock.Of<ICommunityDailyRollupRepository>(),
            Mock.Of<ICommunityDashboardAnalyticsService>(),
            Mock.Of<ICommunityPostBoostService>(),
            authorization.Object,
            Mock.Of<INotificationService>(),
            UserBlockTestHelpers.CreateVisibilityMock().Object);

        var identity = viewerUserId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, viewerUserId.Value.ToString()) }, "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }
}
