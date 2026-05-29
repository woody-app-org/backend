using Moq;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class NotificationServicePostSharedTests
{
    [Fact]
    public async Task NotifyPostSharedAsync_CreatesNotification_ForPostOwner()
    {
        var notifications = new Mock<INotificationRepository>();
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = 10, PublicId = "pst_test00000010" });

        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdNoTrackingAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 2,
                Username = "autora",
                Email = "a@t.com",
                VerificationStatus = VerificationStatus.Approved
            });

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService(notifications, visibility, users, posts);

        await svc.NotifyPostSharedAsync(1, 2, 10, CancellationToken.None);

        notifications.Verify(
            x => x.Add(It.Is<Notification>(n =>
                n.RecipientUserId == 2 &&
                n.ActorUserId == 1 &&
                n.Type == NotificationType.PostShared &&
                n.TargetKind == NotificationTargetKind.Post &&
                n.TargetId == 10)),
            Times.Once);
        notifications.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyPostSharedAsync_DoesNotCreate_WhenActorIsPostOwner()
    {
        var notifications = new Mock<INotificationRepository>();
        var svc = CreateService(notifications, new Mock<IUserRelationshipVisibilityService>(), new Mock<IUserRepository>(), new Mock<IPostRepository>());

        await svc.NotifyPostSharedAsync(2, 2, 10, CancellationToken.None);

        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task NotifyPostSharedAsync_DoesNotCreate_WhenUsersBlockedEitherWay()
    {
        var notifications = new Mock<INotificationRepository>();
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService(notifications, visibility, new Mock<IUserRepository>(), new Mock<IPostRepository>());

        await svc.NotifyPostSharedAsync(1, 2, 10, CancellationToken.None);

        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task NotifyPostSharedAsync_DoesNotCreate_WhenOwnerNotApproved()
    {
        var notifications = new Mock<INotificationRepository>();
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdNoTrackingAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 2,
                Username = "autora",
                Email = "a@t.com",
                VerificationStatus = VerificationStatus.PendingReview
            });

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService(notifications, visibility, users, new Mock<IPostRepository>());

        await svc.NotifyPostSharedAsync(1, 2, 10, CancellationToken.None);

        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task NotifyPostSharedAsync_DoesNotCreate_WhenPostDeleted()
    {
        var notifications = new Mock<INotificationRepository>();
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdNoTrackingAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 2,
                Username = "autora",
                Email = "a@t.com",
                VerificationStatus = VerificationStatus.Approved
            });

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService(notifications, visibility, users, posts);

        await svc.NotifyPostSharedAsync(1, 2, 10, CancellationToken.None);

        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
    }

    private static NotificationService CreateService(
        Mock<INotificationRepository> notifications,
        Mock<IUserRelationshipVisibilityService> visibility,
        Mock<IUserRepository> users,
        Mock<IPostRepository> posts) =>
        new(
            notifications.Object,
            new Mock<INotificationRealtimePublisher>().Object,
            users.Object,
            posts.Object,
            visibility.Object);
}
