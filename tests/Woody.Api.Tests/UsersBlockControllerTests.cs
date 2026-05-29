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

public class UsersBlockControllerTests
{
    [Fact]
    public async Task BlockUser_RequiresAuth()
    {
        var controller = CreateController(visibility: new Mock<IUserRelationshipVisibilityService>(), userId: null);

        var result = await controller.BlockUser("2", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task BlockUser_BlocksSuccessfully_ReturnsNoContent()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.BlockAsync(7, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(visibility, userId: 7);

        var result = await controller.BlockUser("2", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        visibility.Verify(v => v.BlockAsync(7, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlockUser_SelfBlock_ReturnsBadRequest()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        var controller = CreateController(visibility, userId: 7);

        var result = await controller.BlockUser("7", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        visibility.Verify(v => v.BlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BlockUser_MissingUser_ReturnsNotFound()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.BlockAsync(7, 404, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var controller = CreateController(visibility, userId: 7);

        var result = await controller.BlockUser("404", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UnblockUser_IsIdempotent_ReturnsNoContent()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.UnblockAsync(7, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(visibility, userId: 7);

        var result = await controller.UnblockUser("2", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        visibility.Verify(v => v.UnblockAsync(7, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyBlockedUsers_ReturnsOnlyMyBlockedList()
    {
        var blocked = new UserPublicDto { Id = "2", Username = "blocked_user", Name = "Blocked User" };
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.ListBlockedByUserPagedAsync(7, 1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponseDto<UserPublicDto>
            {
                Items = [blocked],
                Page = 1,
                PageSize = 20,
                TotalCount = 1,
                HasNextPage = false,
                HasPreviousPage = false
            });

        var controller = CreateController(visibility, userId: 7);

        var result = await controller.GetMyBlockedUsers(page: 1, pageSize: 20, search: null, cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PaginatedResponseDto<UserPublicDto>>(ok.Value);
        Assert.Single(dto.Items);
        Assert.Equal("blocked_user", dto.Items[0].Username);
        visibility.Verify(v => v.ListBlockedByUserPagedAsync(7, 1, 20, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyBlockedUsers_PassesNormalizedSearch()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.ListBlockedByUserPagedAsync(7, 1, 10, "ana", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponseDto<UserPublicDto>
            {
                Items = [],
                Page = 1,
                PageSize = 10,
                TotalCount = 0,
                HasNextPage = false,
                HasPreviousPage = false
            });

        var controller = CreateController(visibility, userId: 7);

        await controller.GetMyBlockedUsers(page: 1, pageSize: 10, search: " @Ana ", cancellationToken: CancellationToken.None);

        visibility.Verify(v => v.ListBlockedByUserPagedAsync(7, 1, 10, "ana", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyBlockedUsers_ClampPageSize()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.ListBlockedByUserPagedAsync(7, 1, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponseDto<UserPublicDto>
            {
                Items = [],
                Page = 1,
                PageSize = 50,
                TotalCount = 0,
                HasNextPage = false,
                HasPreviousPage = false
            });

        var controller = CreateController(visibility, userId: 7);

        await controller.GetMyBlockedUsers(page: 1, pageSize: 500, search: null, cancellationToken: CancellationToken.None);

        visibility.Verify(v => v.ListBlockedByUserPagedAsync(7, 1, 50, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UsersController CreateController(Mock<IUserRelationshipVisibilityService> visibility, int? userId)
    {
        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.AddAsync(It.IsAny<UsernameHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        history.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var users = new Mock<IUserRepository>();
        var badgeAward = new Mock<IBadgeAwardService>();
        badgeAward.Setup(x => x.GetUserBadgesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBadgeDto>());

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

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
            new Mock<IFollowRepository>().Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<INotificationService>().Object,
            stories.Object,
            badgeAward.Object,
            visibility.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
