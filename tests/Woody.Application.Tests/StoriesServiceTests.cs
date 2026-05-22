using Moq;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Application.Stories;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class StoriesServiceTests
{
    private readonly Mock<IStoryRepository> _stories = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IFollowRepository> _follows = new();
    private readonly Mock<IMediaStorageProvider> _media = new();
    private readonly Mock<IProfileSignalSocialGate> _gate = new();

    public StoriesServiceTests()
    {
        _gate.Setup(g => g.AreUsersBlockedEitherWayAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task CreateStoryAsync_Image_Valid_ReturnsSuccess()
    {
        var user = new User { Id = 1, Username = "ana", DisplayName = "Ana" };
        _users.Setup(u => u.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _stories.Setup(s => s.CreateWithActiveLimitAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Story st, CancellationToken _) => st);

        var svc = CreateService();
        var result = await svc.CreateStoryAsync(1, new CreateStoryRequestDto
        {
            MediaType = "image",
            MediaUrl = "https://cdn.example.com/posts/1/photo.jpg"
        });

        Assert.Equal(StoryOperationOutcome.Success, result.Outcome);
        Assert.NotNull(result.Story);
        Assert.Equal("image", result.Story!.MediaType);
    }

    [Fact]
    public async Task CreateStoryAsync_Text_Valid_ReturnsSuccess()
    {
        var user = new User { Id = 1, Username = "ana" };
        _users.Setup(u => u.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _stories.Setup(s => s.CreateWithActiveLimitAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Story st, CancellationToken _) => st);

        var svc = CreateService();
        var result = await svc.CreateStoryAsync(1, new CreateStoryRequestDto
        {
            MediaType = "text",
            Text = "Olá!",
            BackgroundColor = "#FF5733"
        });

        Assert.Equal(StoryOperationOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task CreateStoryAsync_EmptyContent_ReturnsInvalidContent()
    {
        var svc = CreateService();
        var result = await svc.CreateStoryAsync(1, new CreateStoryRequestDto { MediaType = "text", Text = "   " });
        Assert.Equal(StoryOperationOutcome.InvalidContent, result.Outcome);
    }

    [Fact]
    public async Task CreateStoryAsync_InvalidMediaType_ReturnsInvalidMediaType()
    {
        var svc = CreateService();
        var result = await svc.CreateStoryAsync(1, new CreateStoryRequestDto { MediaType = "audio" });
        Assert.Equal(StoryOperationOutcome.InvalidMediaType, result.Outcome);
    }

    [Fact]
    public async Task CreateStoryAsync_UnsafeMediaUrl_ReturnsInvalidContent()
    {
        var svc = CreateService();
        var result = await svc.CreateStoryAsync(1, new CreateStoryRequestDto
        {
            MediaType = "image",
            MediaUrl = "javascript:alert(1)"
        });
        Assert.Equal(StoryOperationOutcome.InvalidContent, result.Outcome);
    }

    [Fact]
    public async Task CreateStoryAsync_LimitReached_ReturnsLimitReachedWithCode()
    {
        _users.Setup(u => u.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Username = "ana" });
        _stories.Setup(s => s.CreateWithActiveLimitAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StoryLimitReachedException());

        var svc = CreateService();
        var result = await svc.CreateStoryAsync(1, new CreateStoryRequestDto
        {
            MediaType = "text",
            Text = "Mais um"
        });

        Assert.Equal(StoryOperationOutcome.LimitReached, result.Outcome);
        Assert.Equal(StoryLimitReachedException.ErrorCode, result.Code);
    }

    [Fact]
    public async Task DeleteStoryAsync_OtherUser_ReturnsNotFound()
    {
        var story = new Story
        {
            Id = 5,
            AuthorUserId = 2,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            DeletedAt = null
        };
        _stories.Setup(s => s.GetByIdIncludingDeletedAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(story);
        _users.Setup(u => u.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, Role = "User" });

        var svc = CreateService();
        var result = await svc.DeleteStoryAsync(1, 5);
        Assert.Equal(StoryOperationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task GetActiveStoriesByUserAsync_DoesNotReturnExpiredStories()
    {
        _users.Setup(u => u.GetByIdNoTrackingAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 5, Username = "maria" });
        _stories.Setup(s => s.ListActiveByAuthorAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var svc = CreateService();
        var list = await svc.GetActiveStoriesByUserAsync(5, viewerUserId: 1);
        Assert.Empty(list);
    }

    // -------------------------------------------------------------------------
    // IDOR: block check agora é aplicado (fix fase 4)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetActiveStoriesByUserAsync_ReturnsEmpty_WhenViewerIsBlocked()
    {
        _users.Setup(u => u.GetByIdNoTrackingAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 5, Username = "blocked-user" });

        // Viewe 1 está bloqueado pelo utilizador 5 (ou vice-versa).
        _gate.Setup(g => g.AreUsersBlockedEitherWayAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService();
        var list = await svc.GetActiveStoriesByUserAsync(5, viewerUserId: 1);

        Assert.Empty(list);
        // Repository de stories NÃO deve ser consultado quando há bloqueio.
        _stories.Verify(s => s.ListActiveByAuthorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveStoriesByUserAsync_ReturnsStories_WhenNotBlocked()
    {
        var author = new User { Id = 5, Username = "author" };
        var story = new Story
        {
            Id = 10,
            AuthorUserId = 5,
            Author = author,
            Visibility = StoryVisibility.Public,
            ExpiresAt = DateTime.UtcNow.AddHours(6),
            MediaType = StoryMediaType.Text
        };

        _users.Setup(u => u.GetByIdNoTrackingAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        // Não bloqueado → gate retorna false (default do setUp geral).
        _stories.Setup(s => s.ListActiveByAuthorAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([story]);
        _stories.Setup(s => s.GetViewCountsByStoryIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 3 });
        _stories.Setup(s => s.GetStoryIdsViewedByUserAsync(1, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var svc = CreateService();
        var list = await svc.GetActiveStoriesByUserAsync(5, viewerUserId: 1);

        Assert.Single(list);
    }

    [Fact]
    public async Task GetActiveStoriesByUserAsync_ReturnsStories_ForOwnProfile()
    {
        var author = new User { Id = 5, Username = "self" };
        var story = new Story
        {
            Id = 11,
            AuthorUserId = 5,
            Author = author,
            Visibility = StoryVisibility.Public,
            ExpiresAt = DateTime.UtcNow.AddHours(6),
            MediaType = StoryMediaType.Text
        };

        _users.Setup(u => u.GetByIdNoTrackingAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);
        _stories.Setup(s => s.ListActiveByAuthorAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([story]);
        _stories.Setup(s => s.GetViewCountsByStoryIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());
        _stories.Setup(s => s.GetStoryIdsViewedByUserAsync(5, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var svc = CreateService();
        // viewer == target → bloco nunca deve ser verificado
        var list = await svc.GetActiveStoriesByUserAsync(5, viewerUserId: 5);

        Assert.Single(list);
        _gate.Verify(g => g.AreUsersBlockedEitherWayAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStoryViewsAsync_NonAuthor_ReturnsForbidden()
    {
        var story = new Story
        {
            Id = 1,
            AuthorUserId = 2,
            Author = new User { Id = 2, Username = "b" },
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _stories.Setup(s => s.GetActiveByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(story);

        var svc = CreateService();
        var result = await svc.GetStoryViewsAsync(1, 1);
        Assert.Equal(StoryOperationOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task GetStoriesFeedAsync_OrdersSelfFirstThenByRecency()
    {
        var now = DateTime.UtcNow;
        _follows.Setup(f => f.GetFollowedUserIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([2]);
        _stories.Setup(s => s.ListActiveStoryAuthorsByUserIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new StoryFeedAuthorSummary
                {
                    AuthorUserId = 2,
                    LastCreatedAt = now.AddMinutes(-5),
                    StoryIds = [20]
                },
                new StoryFeedAuthorSummary
                {
                    AuthorUserId = 1,
                    LastCreatedAt = now.AddHours(-1),
                    StoryIds = [10]
                }
            ]);
        _stories.Setup(s => s.GetStoryIdsViewedByUserAsync(1, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        _users.Setup(u => u.GetByIdsNoTrackingAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new User { Id = 1, Username = "me", DisplayName = "Me" },
                new User { Id = 2, Username = "friend", DisplayName = "Friend" }
            ]);

        var svc = CreateService();
        var feed = await svc.GetStoriesFeedAsync(1);

        Assert.Equal(2, feed.Count);
        Assert.True(feed[0].IsSelf);
        Assert.Equal("1", feed[0].UserId);
        Assert.False(feed[0].HasUnviewedStories);
        Assert.True(feed[1].HasUnviewedStories);
    }

    private StoriesService CreateService() =>
        new(_stories.Object, _users.Object, _follows.Object, _media.Object, _gate.Object);
}
