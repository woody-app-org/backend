using Moq;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;

namespace Woody.Application.Tests;

public class BadgeAwardServiceTests
{
    private readonly Mock<IBadgeRepository> _badges = new();

    [Fact]
    public async Task GetUserBadgesAsync_ReturnsEmpty_WhenUserHasNoBadges()
    {
        _badges.Setup(x => x.GetActiveUserBadgesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBadge>());

        var service = new BadgeAwardService(_badges.Object);
        var result = await service.GetUserBadgesAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserBadgesAsync_MapsPublicFieldsWithoutInternalIds()
    {
        var earnedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        _badges.Setup(x => x.GetActiveUserBadgesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBadge>
            {
                new()
                {
                    Id = 99,
                    UserId = 1,
                    BadgeId = 7,
                    EarnedAt = earnedAt,
                    MetadataJson = "{\"internal\":true}",
                    Badge = new Badge
                    {
                        Id = 7,
                        Slug = "seed",
                        Name = "Seed",
                        Description = "Presente desde o primeiro dia da Woody.",
                        IconAssetKey = "seed",
                        Category = "founding",
                        Rarity = "founder",
                        IsActive = true,
                        SortOrder = 10,
                        CreatedAt = earnedAt
                    }
                }
            });

        var service = new BadgeAwardService(_badges.Object);
        var result = await service.GetUserBadgesAsync(1);

        var dto = Assert.Single(result);
        Assert.Equal("seed", dto.Slug);
        Assert.Equal("Seed", dto.Name);
        Assert.Equal("Presente desde o primeiro dia da Woody.", dto.Description);
        Assert.Equal("seed", dto.IconAssetKey);
        Assert.Equal("founding", dto.Category);
        Assert.Equal("founder", dto.Rarity);
        Assert.Equal(earnedAt, dto.EarnedAt);
    }

    [Fact]
    public async Task AwardBadgeAsync_ReturnsAlreadyOwned_WhenUserAlreadyHasBadge()
    {
        _badges.Setup(x => x.GetBySlugAsync("seed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Badge { Id = 1, Slug = "seed", IsActive = true });
        _badges.Setup(x => x.UserHasBadgeAsync(1, "seed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new BadgeAwardService(_badges.Object);
        var outcome = await service.AwardBadgeAsync(1, "seed");

        Assert.Equal(BadgeAwardOutcome.AlreadyOwned, outcome);
        _badges.Verify(
            x => x.TryAddUserBadgeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AwardBadgeAsync_IsIdempotent_OnDuplicateInsert()
    {
        _badges.Setup(x => x.GetBySlugAsync("seed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Badge { Id = 1, Slug = "seed", IsActive = true });
        _badges.Setup(x => x.UserHasBadgeAsync(1, "seed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _badges.Setup(x => x.TryAddUserBadgeAsync(1, 1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new BadgeAwardService(_badges.Object);
        var outcome = await service.AwardBadgeAsync(1, "seed");

        Assert.Equal(BadgeAwardOutcome.AlreadyOwned, outcome);
    }

    [Fact]
    public async Task AwardBadgeAsync_ReturnsBadgeInactive_WhenDefinitionIsInactive()
    {
        _badges.Setup(x => x.GetBySlugAsync("seed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Badge { Id = 1, Slug = "seed", IsActive = false });

        var service = new BadgeAwardService(_badges.Object);
        var outcome = await service.AwardBadgeAsync(1, "seed");

        Assert.Equal(BadgeAwardOutcome.BadgeInactive, outcome);
    }

    [Fact]
    public async Task AwardBadgeAsync_ReturnsBadgeNotFound_WhenSlugMissing()
    {
        _badges.Setup(x => x.GetBySlugAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Badge?)null);

        var service = new BadgeAwardService(_badges.Object);
        var outcome = await service.AwardBadgeAsync(1, "missing");

        Assert.Equal(BadgeAwardOutcome.BadgeNotFound, outcome);
    }
}
