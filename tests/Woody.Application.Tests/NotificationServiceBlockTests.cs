using Moq;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class NotificationServiceBlockTests
{
    [Fact]
    public async Task NotifyNewFollowerAsync_DoesNotCreate_WhenUsersBlockedEitherWay()
    {
        var notifications = new Mock<INotificationRepository>();
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService(notifications, visibility);

        await svc.NotifyNewFollowerAsync(1, 2, CancellationToken.None);

        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
        notifications.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListMineAsync_ExcludesNotificationsFromHiddenActors()
    {
        var hidden = new HashSet<int> { 5 };
        var visible = new Notification
        {
            Id = 1,
            RecipientUserId = 1,
            ActorUserId = 9,
            Type = NotificationType.NewFollower,
            TargetKind = NotificationTargetKind.User,
            CreatedAt = DateTime.UtcNow,
            ActorUser = new User { Id = 9, Username = "visible" }
        };

        var notifications = new Mock<INotificationRepository>();
        notifications
            .Setup(x => x.ListForRecipientPagedAsync(
                1,
                1,
                20,
                It.Is<IReadOnlyCollection<int>>(exclude => exclude.Contains(5)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([visible], 1));

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var svc = CreateService(notifications, visibility);

        var result = await svc.ListMineAsync(1, 1, 20, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("1", result.Items[0].Id);
    }

    [Fact]
    public async Task GetUnreadCountAsync_PassesHiddenActorIdsToRepository()
    {
        var hidden = new HashSet<int> { 5, 6 };
        var notifications = new Mock<INotificationRepository>();
        notifications
            .Setup(x => x.CountUnreadForRecipientAsync(
                1,
                It.Is<IReadOnlyCollection<int>>(exclude => exclude.Contains(5) && exclude.Contains(6)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var svc = CreateService(notifications, visibility);

        var count = await svc.GetUnreadCountAsync(1, CancellationToken.None);

        Assert.Equal(0, count);
    }

    private static NotificationService CreateService(
        Mock<INotificationRepository> notifications,
        Mock<IUserRelationshipVisibilityService> visibility) =>
        new(
            notifications.Object,
            new Mock<INotificationRealtimePublisher>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<IPostRepository>().Object,
            visibility.Object);
}
