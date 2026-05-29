using Moq;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class FeedServiceBlockTests
{
    [Fact]
    public async Task GetFeedAsync_ExcludesPostsFromHiddenUsers()
    {
        var hidden = new HashSet<int> { 5 };
        var candidates = new List<PostFeedCandidate>
        {
            new(1, 5, PostPublicationContext.Profile, null, DateTime.UtcNow, 0),
            new(2, 9, PostPublicationContext.Profile, null, DateTime.UtcNow.AddMinutes(-1), 0)
        };

        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.ListNonDeletedVisibleFeedCandidatesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        posts.Setup(x => x.ListNonDeletedByIdsWithNavOrderedAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<int> ids, CancellationToken _) =>
                ids.Select(id => new Post
                {
                    Id = id,
                    UserId = id == 1 ? 5 : 9,
                    User = new User { Id = id == 1 ? 5 : 9, Username = "u" },
                    Content = "x",
                    CreatedAt = DateTime.UtcNow
                }).ToList());

        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var follows = new Mock<IFollowRepository>();
        follows.Setup(x => x.GetFollowedUserIdsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());

        var memberships = new Mock<ICommunityMembershipRepository>();
        memberships.Setup(x => x.GetActiveCommunityIdsForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());

        var likes = new Mock<ILikeRepository>();
        likes.Setup(x => x.GetPostLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetActiveCommentCountsByPostIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var boosts = new Mock<ICommunityPostBoostRepository>();
        boosts.Setup(x => x.GetActiveBoostedPostIdsAmongAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment
            .Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Post> source, int? _, CancellationToken _) =>
                source.Select(p => new PostResponseDto { Id = p.Id.ToString(), AuthorId = p.UserId.ToString() }).ToList());

        var svc = new FeedService(
            posts.Object,
            follows.Object,
            memberships.Object,
            likes.Object,
            comments.Object,
            enrichment.Object,
            boosts.Object,
            visibility.Object);

        var result = await svc.GetFeedAsync(1, 10, "trending", 1, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("2", result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetFeedAsync_DoesNotFilterHiddenUsers_WhenAnonymous()
    {
        var candidates = new List<PostFeedCandidate>
        {
            new(1, 5, PostPublicationContext.Profile, null, DateTime.UtcNow, 0),
            new(2, 9, PostPublicationContext.Profile, null, DateTime.UtcNow.AddMinutes(-1), 0)
        };

        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.ListNonDeletedVisibleFeedCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        posts.Setup(x => x.ListNonDeletedByIdsWithNavOrderedAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates.Select(c => new Post
            {
                Id = c.Id,
                UserId = c.UserId,
                User = new User { Id = c.UserId, Username = "u" },
                Content = "x",
                CreatedAt = c.CreatedAt
            }).ToList());

        var visibility = new Mock<IUserRelationshipVisibilityService>(MockBehavior.Strict);

        var likes = new Mock<ILikeRepository>();
        likes.Setup(x => x.GetPostLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetActiveCommentCountsByPostIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var boosts = new Mock<ICommunityPostBoostRepository>();
        boosts.Setup(x => x.GetActiveBoostedPostIdsAmongAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment
            .Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Post> source, int? _, CancellationToken _) =>
                source.Select(p => new PostResponseDto { Id = p.Id.ToString() }).ToList());

        var svc = new FeedService(
            posts.Object,
            new Mock<IFollowRepository>().Object,
            new Mock<ICommunityMembershipRepository>().Object,
            likes.Object,
            comments.Object,
            enrichment.Object,
            boosts.Object,
            visibility.Object);

        var result = await svc.GetFeedAsync(1, 10, "trending", null, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        visibility.Verify(v => v.GetHiddenUserIdsForViewerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
