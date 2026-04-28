using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.Billing;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Tests;

public class PrivateReadAuthorizationTests
{
    [Fact]
    public async Task Search_DoesNotReturnPrivateCommunityPost_ForAnonymousViewer()
    {
        var publicPost = CreatePost(1, null, "public result");
        var privatePost = CreatePost(2, 10, "private result", visibility: "private");
        var controller = CreateSearchController(new[] { publicPost, privatePost }, allowedPostIds: new[] { 1 });

        var result = await controller.Search("result", "posts", CancellationToken.None);

        var posts = ExtractSearchPosts(result);
        Assert.Single(posts);
        Assert.Equal("1", posts[0].Id);
    }

    [Fact]
    public async Task Search_DoesNotReturnPrivateCommunityPost_ForAuthenticatedNonMember()
    {
        var publicPost = CreatePost(1, null, "public result");
        var privatePost = CreatePost(2, 10, "private result", visibility: "private");
        var controller = CreateSearchController(new[] { publicPost, privatePost }, allowedPostIds: new[] { 1 }, viewerUserId: 20);

        var result = await controller.Search("result", "posts", CancellationToken.None);

        var posts = ExtractSearchPosts(result);
        Assert.Single(posts);
        Assert.Equal("1", posts[0].Id);
    }

    [Fact]
    public async Task Search_ReturnsPrivateCommunityPost_ForAuthorizedViewer()
    {
        var publicPost = CreatePost(1, null, "public result");
        var privatePost = CreatePost(2, 10, "private result", visibility: "private");
        var controller = CreateSearchController(new[] { publicPost, privatePost }, allowedPostIds: new[] { 1, 2 }, viewerUserId: 20);

        var result = await controller.Search("result", "posts", CancellationToken.None);

        var posts = ExtractSearchPosts(result);
        Assert.Equal(new[] { "1", "2" }, posts.Select(p => p.Id).ToArray());
    }

    [Fact]
    public async Task Search_PeopleAndCommunitiesModes_DoNotUsePostAuthorization()
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.SearchUsersNoTrackingAsync("woody", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { CreateUser(7, "woody") });

        var communities = new Mock<ICommunityRepository>();
        communities
            .Setup(x => x.SearchWithTagsAsync("woody", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Community> { CreateCommunity(3, "public") });

        var posts = new Mock<IPostRepository>();
        var enrichment = new Mock<IPostEnrichmentService>();
        var authorization = new Mock<IResourceAuthorizationService>(MockBehavior.Strict);
        var controller = new SearchController(users.Object, communities.Object, posts.Object, enrichment.Object, authorization.Object);
        SetUser(controller, userId: null);

        var people = await controller.Search("woody", "people", CancellationToken.None);
        var communityResults = await controller.Search("woody", "communities", CancellationToken.None);

        Assert.IsType<OkObjectResult>(people);
        Assert.IsType<OkObjectResult>(communityResults);
        authorization.Verify(
            x => x.CanReadPostAsync(It.IsAny<Post>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(20)]
    public async Task CommunityMembers_BlocksPrivateCommunity_ForAnonymousOrNonMember(int? viewerUserId)
    {
        var controller = CreateCommunitiesController(
            CreateCommunity(10, "private"),
            canReadMembers: false,
            viewerUserId: viewerUserId);

        var result = await controller.Members("10", cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CommunityMembers_ReturnsPrivateMembers_ForAuthorizedViewer()
    {
        var controller = CreateCommunitiesController(
            CreateCommunity(10, "private"),
            canReadMembers: true,
            viewerUserId: 20,
            members: new[]
            {
                new CommunityMembership { UserId = 20, CommunityId = 10, Role = "member", Status = "active", User = CreateUser(20, "member") }
            });

        var result = await controller.Members("10", cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResponseDto<CommunityMemberItemDto>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal("20", payload.Items[0].User.Id);
    }

    [Fact]
    public async Task CommunityMembers_ReturnsPublicMembers_ForAnonymousViewer()
    {
        var controller = CreateCommunitiesController(
            CreateCommunity(10, "public"),
            canReadMembers: true,
            members: new[]
            {
                new CommunityMembership { UserId = 21, CommunityId = 10, Role = "member", Status = "active", User = CreateUser(21, "public-member") }
            });

        var result = await controller.Members("10", cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResponseDto<CommunityMemberItemDto>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal("21", payload.Items[0].User.Id);
    }

    private static SearchController CreateSearchController(
        IReadOnlyList<Post> searchResults,
        IReadOnlyCollection<int> allowedPostIds,
        int? viewerUserId = null)
    {
        var users = new Mock<IUserRepository>();
        var communities = new Mock<ICommunityRepository>();
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.SearchNonDeletedWithNavAsync("result", 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults.ToList());

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment
            .Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), viewerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Post> source, int? _, CancellationToken _) =>
                source.Select(p => new PostResponseDto
                {
                    Id = p.Id.ToString(),
                    PublicationContext = p.PublicationContext == PostPublicationContext.Profile ? "profile" : "community",
                    AuthorId = p.UserId.ToString(),
                    Author = new UserPublicDto { Id = p.UserId.ToString(), Username = p.User.Username, Name = p.User.Username },
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt.ToString("o")
                }).ToList());

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), viewerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post post, int? _, CancellationToken _) => allowedPostIds.Contains(post.Id));

        var controller = new SearchController(users.Object, communities.Object, posts.Object, enrichment.Object, authorization.Object);
        SetUser(controller, viewerUserId);
        return controller;
    }

    private static CommunitiesController CreateCommunitiesController(
        Community community,
        bool canReadMembers,
        int? viewerUserId = null,
        IEnumerable<CommunityMembership>? members = null)
    {
        var communities = new Mock<ICommunityRepository>();
        communities
            .Setup(x => x.GetByIdWithTagsNoTrackingAsync(community.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(community);

        var memberRows = members?.ToList() ?? new List<CommunityMembership>();
        var memberships = new Mock<ICommunityMembershipRepository>();
        memberships
            .Setup(x => x.ListActiveMembersPagedOrderedAsync(community.Id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((memberRows, memberRows.Count));

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadCommunityMembersAsync(community, viewerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canReadMembers);

        var controller = new CommunitiesController(
            communities.Object,
            memberships.Object,
            Mock.Of<IJoinRequestRepository>(),
            Mock.Of<IPostRepository>(),
            Mock.Of<IPostEnrichmentService>(),
            Mock.Of<ICommunityPermissionService>(),
            Mock.Of<IUserEntitlementService>(),
            Mock.Of<ICommunitySubscriptionRepository>(),
            Mock.Of<ICommunityPremiumEntitlementService>(),
            Mock.Of<ICommunityDailyRollupRepository>(),
            Mock.Of<ICommunityDashboardAnalyticsService>(),
            Mock.Of<ICommunityPostBoostService>(),
            authorization.Object);

        SetUser(controller, viewerUserId);
        return controller;
    }

    private static List<PostResponseDto> ExtractSearchPosts(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var property = ok.Value!.GetType().GetProperty("posts");
        Assert.NotNull(property);
        return Assert.IsType<List<PostResponseDto>>(property!.GetValue(ok.Value));
    }

    private static Post CreatePost(int id, int? communityId, string title, string visibility = "public")
    {
        var author = CreateUser(1, "author");
        return new Post
        {
            Id = id,
            UserId = author.Id,
            User = author,
            Title = title,
            Content = title,
            CreatedAt = DateTime.UtcNow,
            PublicationContext = communityId.HasValue ? PostPublicationContext.Community : PostPublicationContext.Profile,
            CommunityId = communityId,
            Community = communityId.HasValue ? CreateCommunity(communityId.Value, visibility) : null
        };
    }

    private static Community CreateCommunity(int id, string visibility) => new()
    {
        Id = id,
        Slug = $"community-{id}",
        Name = $"Community {id}",
        Description = "Description",
        Category = "general",
        Visibility = visibility,
        OwnerUserId = 1
    };

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
        DisplayName = username,
        Email = $"{username}@example.com",
        Role = "User"
    };

    private static void SetUser(ControllerBase controller, int? userId)
    {
        var identity = userId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
