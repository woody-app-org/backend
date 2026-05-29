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

    [Fact]
    public async Task GetByUsername_ReturnsBadges_WhenUserHasEarnedBadges()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByUsernameAsync("badge_user")).ReturnsAsync(new User
        {
            Id = 42,
            Username = "badge_user",
            DisplayName = "Badge User",
            Email = "b@example.com",
            Role = "User",
            Bio = string.Empty
        });
        users.Setup(x => x.GetByIdWithSocialLinksAndInterestsNoTrackingAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 42,
                Username = "badge_user",
                DisplayName = "Badge User",
                Email = "b@example.com",
                Role = "User",
                Bio = string.Empty
            });

        var history = new Mock<IUsernameHistoryRepository>();
        var earnedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var badgeAward = new Mock<IBadgeAwardService>();
        badgeAward.Setup(x => x.GetUserBadgesAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBadgeDto>
            {
                new()
                {
                    Slug = "seed",
                    Name = "Raiz",
                    Description = "Presente desde o primeiro dia da Woody.",
                    IconAssetKey = "seed",
                    Category = "founding",
                    Rarity = "founder",
                    EarnedAt = earnedAt
                }
            });

        var controller = CreateController(users, history, badgeAward);

        var result = await controller.GetByUsername("badge_user", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileDto>(ok.Value);
        var badge = Assert.Single(dto.Badges);
        Assert.Equal("seed", badge.Slug);
        Assert.Equal(earnedAt, badge.EarnedAt);
    }

    private static UsersController CreateController(
        Mock<IUserRepository> users,
        Mock<IUsernameHistoryRepository> history,
        Mock<IBadgeAwardService>? badgeAward = null)
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

        var badgeAwardService = badgeAward ?? new Mock<IBadgeAwardService>();
        if (badgeAward == null)
        {
            badgeAwardService.Setup(x => x.GetUserBadgesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserBadgeDto>());
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
            badgeAwardService.Object,
            new Mock<IUserRelationshipVisibilityService>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
