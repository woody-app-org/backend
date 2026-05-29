using Moq;
using Woody.Application.DTOs.Api;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class PostSharingServiceTests
{
    private const string GenericError = "Não foi possível compartilhar esta publicação.";

    [Fact]
    public async Task ShareToConversationAsync_Succeeds_ForPublicProfilePost()
    {
        var post = SampleProfilePost(10, authorId: 1);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanReadPostAsync(post, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var conversations = new Mock<IConversationRepository>();

        var dm = new Mock<IDirectMessagingService>();
        dm.Setup(x => x.StartOrGetConversationAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationResponseDto { Id = 99 });
        dm.Setup(x => x.SendSharedPostMessageAsync(1, 99, 10, "olá", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessageResponseDto { Id = 5, ConversationId = 99 });

        var notificationSvc = new Mock<INotificationService>();

        var svc = new PostSharingService(posts.Object, auth.Object, visibility.Object, conversations.Object, dm.Object, notificationSvc.Object);

        var result = await svc.ShareToConversationAsync(1, 10, new SharePostToConversationRequestDto
        {
            RecipientUserId = 2,
            Message = "olá"
        });

        Assert.Equal(99, result.ConversationId);
        Assert.Equal(5, result.Message.Id);
    }

    [Fact]
    public async Task ShareToConversationAsync_Throws_WhenRecipientCannotReadPrivateCommunityPost()
    {
        var post = SampleCommunityPost(11, communityVisibility: "private");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanReadPostAsync(post, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dm = new Mock<IDirectMessagingService>();
        dm.Setup(x => x.StartOrGetConversationAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationResponseDto { Id = 99 });

        var svc = new PostSharingService(
            posts.Object,
            auth.Object,
            visibility.Object,
            new Mock<IConversationRepository>().Object,
            dm.Object,
            new Mock<INotificationService>().Object);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.ShareToConversationAsync(1, 11, new SharePostToConversationRequestDto { RecipientUserId = 2 }));

        Assert.Equal(GenericError, ex.Message);
    }

    [Fact]
    public async Task ShareToConversationAsync_Throws_WhenBlockedEitherWay()
    {
        var post = SampleProfilePost(12, authorId: 3);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanReadPostAsync(post, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dm = new Mock<IDirectMessagingService>();
        dm.Setup(x => x.StartOrGetConversationAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationResponseDto { Id = 99 });

        var svc = new PostSharingService(
            posts.Object,
            auth.Object,
            visibility.Object,
            new Mock<IConversationRepository>().Object,
            dm.Object,
            new Mock<INotificationService>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.ShareToConversationAsync(1, 12, new SharePostToConversationRequestDto { RecipientUserId = 2 }));
    }

    [Fact]
    public async Task ShareToConversationAsync_Throws_WhenBlockedWithPostAuthor()
    {
        var post = SampleProfilePost(13, authorId: 3);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedWithNavAsync(13, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanReadPostAsync(post, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dm = new Mock<IDirectMessagingService>();
        dm.Setup(x => x.StartOrGetConversationAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationResponseDto { Id = 99 });

        var svc = new PostSharingService(
            posts.Object,
            auth.Object,
            visibility.Object,
            new Mock<IConversationRepository>().Object,
            dm.Object,
            new Mock<INotificationService>().Object);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.ShareToConversationAsync(1, 13, new SharePostToConversationRequestDto { RecipientUserId = 2 }));

        Assert.Equal(GenericError, ex.Message);
        dm.Verify(
            x => x.SendSharedPostMessageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
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

    private static Post SampleCommunityPost(int id, string communityVisibility) =>
        new()
        {
            Id = id,
            PublicId = $"pst_test{id:000000}",
            UserId = 1,
            User = new User { Id = 1, Username = "ana", DisplayName = "Ana", Email = "a@t.com" },
            Content = "secret",
            CreatedAt = DateTime.UtcNow,
            PublicationContext = PostPublicationContext.Community,
            CommunityId = 5,
            Community = new Community
            {
                Id = 5,
                Name = "Priv",
                Slug = "priv",
                Visibility = communityVisibility,
                OwnerUserId = 1
            }
        };
}
