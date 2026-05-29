using Moq;
using Woody.Application.DTOs.Api;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class DirectMessagingBlockTests
{
    [Fact]
    public async Task StartOrGetConversationAsync_ThrowsNotFound_WhenBlockedEitherWay()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdNoTrackingAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 2, Username = "bia" });

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var svc = CreateService(users: users, visibility: visibility);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.StartOrGetConversationAsync(1, 2));
        Assert.Equal("Utilizadora não encontrada.", ex.Message);
    }

    [Fact]
    public async Task SendMessageAsync_ThrowsForbidden_WhenBlockedEitherWay()
    {
        var conversation = CreateConversation(10, 1, 2, ConversationStatus.Accepted);
        var conversations = new Mock<IConversationRepository>();
        conversations
            .Setup(x => x.GetTrackedByIdForParticipantAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var svc = CreateService(conversations: conversations, visibility: visibility);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.SendMessageAsync(1, 10, new SendConversationMessageRequestDto { Body = "Olá" }));

        Assert.Equal("Não foi possível enviar a mensagem.", ex.Message);
    }

    [Fact]
    public async Task ListMyConversationsAsync_OmitsConversationsWithHiddenParticipants()
    {
        var conversations = new Mock<IConversationRepository>();
        conversations
            .Setup(x => x.ListMineNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Conversation>
            {
                CreateConversation(10, 1, 2, ConversationStatus.Accepted),
                CreateConversation(11, 1, 9, ConversationStatus.Accepted)
            });

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int> { 2 });

        var messages = new Mock<IMessageRepository>();
        messages
            .Setup(x => x.GetLastMessageSummariesByConversationIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, (string? Preview, DateTime AtUtc)>());

        var svc = CreateService(conversations: conversations, messages: messages, visibility: visibility);

        var result = await svc.ListMyConversationsAsync(1, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(11, result[0].Id);
    }

    private static Conversation CreateConversation(int id, int low, int high, ConversationStatus status) =>
        new()
        {
            Id = id,
            UserLowId = low,
            UserHighId = high,
            UserLow = new User { Id = low, Username = $"user{low}" },
            UserHigh = new User { Id = high, Username = $"user{high}" },
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static DirectMessagingService CreateService(
        Mock<IConversationRepository>? conversations = null,
        Mock<IMessageRepository>? messages = null,
        Mock<IUserRepository>? users = null,
        Mock<IUserRelationshipVisibilityService>? visibility = null)
    {
        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.AreMutualFollowersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var visibilityMock = visibility ?? new Mock<IUserRelationshipVisibilityService>();
        if (visibility == null)
        {
            visibilityMock.Setup(v => v.AreUsersBlockedEitherWayAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            visibilityMock.Setup(v => v.GetHiddenUserIdsForViewerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int>());
        }

        return new DirectMessagingService(
            (conversations ?? new Mock<IConversationRepository>()).Object,
            (messages ?? new Mock<IMessageRepository>()).Object,
            follows.Object,
            (users ?? new Mock<IUserRepository>()).Object,
            new Mock<IDirectMessageRealtimePublisher>().Object,
            new Mock<INotificationService>().Object,
            visibilityMock.Object,
            new Mock<IResourceAuthorizationService>().Object);
    }
}
