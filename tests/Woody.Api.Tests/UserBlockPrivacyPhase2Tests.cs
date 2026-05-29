using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class UserBlockPrivacyPhase2Tests
{
    [Fact]
    public async Task GetByUsername_ReturnsNotFound_WhenBlockedEitherWay()
    {
        var target = CreateUser(1, "alice");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateUsersController(visibility, viewerUserId: 2, users: [target]);

        var result = await controller.GetByUsername("alice", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByUsername_ReturnsProfile_WhenNoBlock()
    {
        var target = CreateUser(1, "alice");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateUsersController(visibility, viewerUserId: 3, users: [target]);

        var result = await controller.GetByUsername("alice", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileDto>(ok.Value);
        Assert.Equal("1", dto.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenBlockerViewsBlockedOutsideList()
    {
        var target = CreateUser(2, "blocked_user");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateUsersController(visibility, viewerUserId: 1, users: [target]);

        var result = await controller.GetById("2", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetUserPosts_ReturnsNotFound_WhenBlockedEitherWay()
    {
        var target = CreateUser(1, "alice");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var posts = new Mock<IPostRepository>();
        var controller = CreateUsersController(visibility, viewerUserId: 2, users: [target], posts: posts);

        var result = await controller.GetUserPosts("1", cancellationToken: CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        posts.Verify(
            x => x.GetProfilePostsPageAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFollowStatus_ReturnsNotFound_WhenBlockedEitherWay()
    {
        var target = CreateUser(1, "alice");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var follows = new Mock<IFollowRepository>();
        var controller = CreateUsersController(visibility, viewerUserId: 2, users: [target], follows: follows);

        var result = await controller.GetFollowStatus("1", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        follows.Verify(x => x.ExistsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Follow_ReturnsNotFound_WhenBlockedEitherWay()
    {
        var target = CreateUser(2, "blocked_user");
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var follows = new Mock<IFollowRepository>();
        var notifications = new Mock<INotificationService>();
        var controller = CreateUsersController(visibility, viewerUserId: 1, users: [target], follows: follows, notifications: notifications);

        var result = await controller.Follow("2", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        follows.Verify(x => x.Add(It.IsAny<Follow>()), Times.Never);
        notifications.Verify(x => x.NotifyNewFollowerAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSuggestions_ExcludesHiddenUserIds()
    {
        var hidden = new HashSet<int> { 5, 6 };
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.GetHiddenUserIdsForViewerAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hidden);

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.GetFollowedUserIdsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());

        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.ListUsersForSuggestionsAsync(
                It.Is<IReadOnlyCollection<int>>(exclude => exclude.Contains(1) && exclude.Contains(5) && exclude.Contains(6)),
                8,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { CreateUser(9, "suggested") });

        var stories = new Mock<IStoryRepository>();
        stories
            .Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var controller = CreateUsersController(visibility, viewerUserId: 1, usersRepo: users, follows: follows, stories: stories);

        var result = await controller.GetSuggestions(take: 8, cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<UserPublicDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("9", list[0].Id);
    }

    [Fact]
    public async Task SearchPeople_PassesHiddenIdsToRepository_ForAuthenticatedViewer()
    {
        var hidden = new HashSet<int> { 5 };
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.SearchUsersNoTrackingAsync(
                "ana",
                50,
                It.Is<IReadOnlyCollection<int>>(exclude => exclude.Contains(5)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { CreateUser(9, "ana_other") });

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.GetHiddenUserIdsForViewerAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hidden);

        var controller = new SearchController(
            users.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IResourceAuthorizationService>().Object,
            visibility.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "1")],
                        "Test"))
                }
            }
        };

        var result = await controller.Search("ana", "people", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        users.Verify(
            x => x.SearchUsersNoTrackingAsync("ana", 50, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchPeople_DoesNotExclude_WhenAnonymous()
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.SearchUsersNoTrackingAsync("ana", 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { CreateUser(5, "ana") });

        var visibility = new Mock<IUserRelationshipVisibilityService>(MockBehavior.Strict);

        var controller = new SearchController(
            users.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IResourceAuthorizationService>().Object,
            visibility.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        await controller.Search("ana", "people", CancellationToken.None);

        visibility.Verify(
            v => v.GetHiddenUserIdsForViewerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

    private static UsersController CreateUsersController(
        Mock<IUserRelationshipVisibilityService> visibility,
        int? viewerUserId,
        IReadOnlyList<User>? users = null,
        Mock<IUserRepository>? usersRepo = null,
        Mock<IFollowRepository>? follows = null,
        Mock<IPostRepository>? posts = null,
        Mock<INotificationService>? notifications = null,
        Mock<IStoryRepository>? stories = null)
    {
        users ??= Array.Empty<User>();

        var usersMock = usersRepo ?? new Mock<IUserRepository>();
        if (usersRepo == null)
        {
            foreach (var user in users)
            {
                usersMock.Setup(x => x.GetByUsernameAsync(user.Username)).ReturnsAsync(user);
                usersMock.Setup(x => x.GetByIdNoTrackingAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
                usersMock.Setup(x => x.GetByIdWithSocialLinksAndInterestsNoTrackingAsync(user.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);
            }
        }

        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.AddAsync(It.IsAny<UsernameHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        history.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var followsMock = follows ?? new Mock<IFollowRepository>();
        followsMock.Setup(x => x.CountFollowersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        followsMock.Setup(x => x.CountFollowingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        followsMock.Setup(x => x.ExistsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var storiesMock = stories ?? new Mock<IStoryRepository>();
        storiesMock.Setup(x => x.HasActiveStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
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
            new Mock<ICommunityMembershipRepository>().Object,
            followsMock.Object,
            (posts ?? new Mock<IPostRepository>()).Object,
            new Mock<IPostEnrichmentService>().Object,
            (notifications ?? new Mock<INotificationService>()).Object,
            storiesMock.Object,
            badgeAward.Object,
            visibility.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
