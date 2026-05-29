using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Tests;

/// <summary>
/// Regressões de privacidade pós-revisão final — posts directos e grafo social.
/// </summary>
public class UserBlockPrivacyFinalReviewTests
{
    [Fact]
    public async Task GetById_ReturnsNotFound_WhenViewerBlockedWithAuthor()
    {
        var post = SampleProfilePost(id: 10, authorId: 2, publicId: "pst_block00001");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auth = new Mock<IResourceAuthorizationService>();
        var enrichment = new Mock<IPostEnrichmentService>();

        var controller = CreatePostsController(posts, auth, enrichment, visibility, viewerUserId: 1);

        var result = await controller.GetById("10", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        enrichment.Verify(
            x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByPublicId_ReturnsNotFound_WhenViewerBlockedWithAuthor()
    {
        var post = SampleProfilePost(id: 11, authorId: 2, publicId: "pst_block00002");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByPublicIdNonDeletedWithNavAsync("pst_block00002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auth = new Mock<IResourceAuthorizationService>();
        var enrichment = new Mock<IPostEnrichmentService>();

        var controller = CreatePostsController(posts, auth, enrichment, visibility, viewerUserId: 1);

        var result = await controller.GetByPublicId("pst_block00002", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetFollowers_ReturnsNotFound_WhenViewerBlockedWithProfileOwner()
    {
        var target = CreateUser(2, "blocked_user");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var follows = new Mock<IFollowRepository>();
        var controller = CreateUsersController(visibility, viewerUserId: 1, users: [target], follows: follows);

        var result = await controller.GetFollowers("2", cancellationToken: CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        follows.Verify(
            x => x.ListFollowersPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserCommunities_ReturnsNotFound_WhenViewerBlockedWithProfileOwner()
    {
        var target = CreateUser(2, "blocked_user");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var memberships = new Mock<ICommunityMembershipRepository>();
        var controller = CreateUsersController(
            visibility,
            viewerUserId: 1,
            users: [target],
            memberships: memberships);

        var result = await controller.GetUserCommunities("2", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        memberships.Verify(
            x => x.ListActiveWithCommunityAndTagsByUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Post SampleProfilePost(int id, int authorId, string publicId)
    {
        var user = new User
        {
            Id = authorId,
            Username = $"user{authorId}",
            DisplayName = "Test",
            Email = $"u{authorId}@test.com",
            Role = "User"
        };
        return new Post
        {
            Id = id,
            PublicId = publicId,
            UserId = authorId,
            User = user,
            Content = "test",
            PublicationContext = PostPublicationContext.Profile,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static User CreateUser(int id, string username) =>
        new()
        {
            Id = id,
            Username = username,
            DisplayName = username,
            Email = $"{username}@example.com",
            Role = "User",
            Bio = string.Empty
        };

    private static PostsController CreatePostsController(
        Mock<IPostRepository> posts,
        Mock<IResourceAuthorizationService> auth,
        Mock<IPostEnrichmentService> enrichment,
        Mock<IUserRelationshipVisibilityService> visibility,
        int? viewerUserId)
    {
        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            new Mock<ILikeRepository>().Object,
            new Mock<ICommentRepository>().Object,
            enrichment.Object,
            new Mock<IContentPinningService>().Object,
            auth.Object,
            new Mock<INotificationService>().Object,
            visibility.Object);

        var identity = viewerUserId.HasValue
            ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, viewerUserId.Value.ToString())], "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private static UsersController CreateUsersController(
        Mock<IUserRelationshipVisibilityService> visibility,
        int? viewerUserId,
        IReadOnlyList<User> users,
        Mock<IFollowRepository>? follows = null,
        Mock<ICommunityMembershipRepository>? memberships = null)
    {
        var usersMock = new Mock<IUserRepository>();
        foreach (var user in users)
        {
            usersMock.Setup(x => x.GetByIdNoTrackingAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        }

        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.AddAsync(It.IsAny<UsernameHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        history.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var followsMock = follows ?? new Mock<IFollowRepository>();
        var storiesMock = new Mock<IStoryRepository>();
        storiesMock
            .Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var badgeAward = new Mock<IBadgeAwardService>();
        badgeAward.Setup(x => x.GetUserBadgesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBadgeDto>());

        var httpContext = new DefaultHttpContext();
        if (viewerUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, viewerUserId.Value.ToString())],
                "Test"));
        }

        return new UsersController(
            usersMock.Object,
            history.Object,
            new UsernameResolver(usersMock.Object, history.Object),
            (memberships ?? new Mock<ICommunityMembershipRepository>()).Object,
            followsMock.Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<INotificationService>().Object,
            storiesMock.Object,
            badgeAward.Object,
            visibility.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
