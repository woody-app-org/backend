using Moq;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class PostSharingServiceNotificationTests
{
    [Fact]
    public async Task ShareToConversationAsync_NotifiesPostOwner_WhenThirdPartyShares()
    {
        var post = SampleProfilePost(10, authorId: 3);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanReadPostAsync(post, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dm = new Mock<IDirectMessagingService>();
        dm.Setup(x => x.StartOrGetConversationAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationResponseDto { Id = 99 });
        dm.Setup(x => x.SendSharedPostMessageAsync(1, 99, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessageResponseDto { Id = 5, ConversationId = 99 });

        var notifications = new Mock<INotificationService>();

        var svc = new PostSharingService(
            posts.Object,
            auth.Object,
            visibility.Object,
            new Mock<IConversationRepository>().Object,
            dm.Object,
            notifications.Object);

        await svc.ShareToConversationAsync(1, 10, new SharePostToConversationRequestDto { RecipientUserId = 2 });

        notifications.Verify(
            x => x.NotifyPostSharedAsync(1, 3, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShareToConversationAsync_DoesNotCallNotify_WhenShareFailsDueToBlock()
    {
        var post = SampleProfilePost(10, authorId: 3);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanReadPostAsync(post, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dm = new Mock<IDirectMessagingService>();
        dm.Setup(x => x.StartOrGetConversationAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationResponseDto { Id = 99 });

        var notifications = new Mock<INotificationService>();

        var svc = new PostSharingService(
            posts.Object,
            auth.Object,
            visibility.Object,
            new Mock<IConversationRepository>().Object,
            dm.Object,
            notifications.Object);

        await Assert.ThrowsAsync<Woody.Application.Exceptions.ForbiddenException>(() =>
            svc.ShareToConversationAsync(1, 10, new SharePostToConversationRequestDto { RecipientUserId = 2 }));

        notifications.Verify(
            x => x.NotifyPostSharedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Post SampleProfilePost(int id, int authorId) =>
        new()
        {
            Id = id,
            PublicId = $"pst_test{id:000000}",
            UserId = authorId,
            User = new User { Id = authorId, Username = "ana", DisplayName = "Ana", Email = "a@t.com" },
            Content = "hello",
            CreatedAt = DateTime.UtcNow,
            PublicationContext = PostPublicationContext.Profile
        };
}
