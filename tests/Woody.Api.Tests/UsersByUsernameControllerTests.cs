using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class UsersByUsernameControllerTests
{
    [Fact]
    public async Task GetByUsername_ReturnsProfile_WhenUsernameExists()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByUsernameAsync("nicholas_navarro")).ReturnsAsync(new User
        {
            Id = 42,
            Username = "nicholas_navarro",
            DisplayName = "Nicholas",
            Email = "n@example.com",
            Role = "User",
            Bio = string.Empty
        });
        users.Setup(x => x.GetByIdWithSocialLinksAndInterestsNoTrackingAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 42,
                Username = "nicholas_navarro",
                DisplayName = "Nicholas",
                Email = "n@example.com",
                Role = "User",
                Bio = string.Empty
            });

        var history = new Mock<IUsernameHistoryRepository>();
        var controller = CreateController(users, history);

        var result = await controller.GetByUsername("nicholas_navarro", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileDto>(ok.Value);
        Assert.Equal("42", dto.Id);
        Assert.Equal("nicholas_navarro", dto.Username);
        Assert.Null(dto.CanonicalUsername);
    }

    [Fact]
    public async Task GetByUsername_ReturnsCanonicalUsername_WhenResolvedViaHistory()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByUsernameAsync("old_name")).ReturnsAsync((User?)null);
        users.Setup(x => x.GetByIdNoTrackingAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 42,
                Username = "nicholas_navarro",
                DisplayName = "Nicholas",
                Email = "n@example.com",
                Role = "User",
                Bio = string.Empty
            });
        users.Setup(x => x.GetByIdWithSocialLinksAndInterestsNoTrackingAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 42,
                Username = "nicholas_navarro",
                DisplayName = "Nicholas",
                Email = "n@example.com",
                Role = "User",
                Bio = string.Empty
            });

        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.GetUserIdByOldUsernameAsync("old_name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var controller = CreateController(users, history);

        var result = await controller.GetByUsername("old_name", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileDto>(ok.Value);
        Assert.Equal("nicholas_navarro", dto.Username);
        Assert.Equal("nicholas_navarro", dto.CanonicalUsername);
    }

    [Fact]
    public async Task GetByUsername_ReturnsNotFound_WhenUnknown()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByUsernameAsync("missing")).ReturnsAsync((User?)null);

        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.GetUserIdByOldUsernameAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var controller = CreateController(users, history);

        var result = await controller.GetByUsername("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static UsersController CreateController(
        Mock<IUserRepository> users,
        Mock<IUsernameHistoryRepository> history)
    {
        history.Setup(x => x.AddAsync(It.IsAny<UsernameHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        history.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.CountFollowersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        follows.Setup(x => x.CountFollowingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.HasActiveStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        return new UsersController(
            users.Object,
            history.Object,
            new UsernameResolver(users.Object, history.Object),
            new Mock<ICommunityMembershipRepository>().Object,
            follows.Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<INotificationService>().Object,
            stories.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
