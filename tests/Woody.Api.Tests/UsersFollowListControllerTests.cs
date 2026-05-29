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

public class UsersFollowListControllerTests
{
    [Fact]
    public async Task GetFollowers_WithoutSearch_PassesNullSearchToRepository()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Username = "profile", Email = "p@x.com", Password = "h", Role = "User" });

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.ListFollowersPagedAsync(1, 1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var controller = CreateController(users, follows, stories);

        var result = await controller.GetFollowers("1", page: 1, pageSize: 20, search: null, cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        follows.Verify(
            x => x.ListFollowersPagedAsync(1, 1, 20, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFollowers_WithSearch_PassesNormalizedSearchToRepository()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Username = "profile", Email = "p@x.com", Password = "h", Role = "User" });

        var follower = new User
        {
            Id = 2,
            Username = "ana_souza",
            DisplayName = "Ana Souza",
            Email = "ana@example.com",
            Password = "h",
            Role = "User"
        };

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.ListFollowersPagedAsync(1, 1, 20, "ana_souza", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User> { follower }, 1));

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var controller = CreateController(users, follows, stories);

        var result = await controller.GetFollowers("1", page: 1, pageSize: 20, search: " @Ana_Souza ", cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PaginatedResponseDto<UserPublicDto>>(ok.Value);
        Assert.Equal(1, dto.TotalCount);
        Assert.Single(dto.Items);
        Assert.Equal("ana_souza", dto.Items[0].Username);
        follows.Verify(
            x => x.ListFollowersPagedAsync(1, 1, 20, "ana_souza", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserFollowingList_WithBlankSearch_PassesNullSearchToRepository()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Username = "profile", Email = "p@x.com", Password = "h", Role = "User" });

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.ListFollowingPagedAsync(1, 1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var controller = CreateController(users, follows, stories);

        await controller.GetUserFollowingList("1", page: 1, pageSize: 20, search: "   ", cancellationToken: CancellationToken.None);

        follows.Verify(
            x => x.ListFollowingPagedAsync(1, 1, 20, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMyFollowing_WithSearch_PassesNormalizedSearchToRepository()
    {
        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.ListFollowingPagedAsync(7, 1, 20, "carla", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var controller = CreateController(new Mock<IUserRepository>(), follows, stories, userId: 7);

        var result = await controller.GetMyFollowing(page: 1, pageSize: 20, search: " Carla ", cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        follows.Verify(
            x => x.ListFollowingPagedAsync(7, 1, 20, "carla", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFollowers_WithSearch_ReturnsFilteredPaginationMetadata()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Username = "profile", Email = "p@x.com", Password = "h", Role = "User" });

        var follower = new User
        {
            Id = 2,
            Username = "ana_souza",
            DisplayName = "Ana Souza",
            Email = "ana@example.com",
            Password = "h",
            Role = "User"
        };

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.ListFollowersPagedAsync(1, 1, 1, "ana", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User> { follower }, 2));

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var controller = CreateController(users, follows, stories);

        var result = await controller.GetFollowers(
            "1",
            page: 1,
            pageSize: 1,
            search: " @Ana ",
            cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PaginatedResponseDto<UserPublicDto>>(ok.Value);
        Assert.Equal(2, dto.TotalCount);
        Assert.True(dto.HasNextPage);
        Assert.False(dto.HasPreviousPage);
        follows.Verify(
            x => x.ListFollowersPagedAsync(1, 1, 1, "ana", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static UsersController CreateController(
        Mock<IUserRepository> users,
        Mock<IFollowRepository> follows,
        Mock<IStoryRepository> stories,
        int? userId = null)
    {
        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.AddAsync(It.IsAny<UsernameHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        history.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var badgeAward = new Mock<IBadgeAwardService>();
        badgeAward.Setup(x => x.GetUserBadgesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBadgeDto>());

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                "Test"));
        }

        return new UsersController(
            users.Object,
            history.Object,
            new UsernameResolver(users.Object, history.Object),
            new Mock<ICommunityMembershipRepository>().Object,
            follows.Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<INotificationService>().Object,
            stories.Object,
            badgeAward.Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
