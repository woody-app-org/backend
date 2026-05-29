using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class UserBlockPrivacyPhase3Tests
{
    [Fact]
    public async Task GetComments_ExcludesHiddenAuthorsAndReplies()
    {
        var hidden = new HashSet<int> { 5 };
        var blockedAuthor = MinimalAuthor(5);
        var visibleAuthor = MinimalAuthor(9);
        var comments = new List<Comment>
        {
            CommentOnPost(1, 100, blockedAuthor),
            CommentOnPost(2, 100, visibleAuthor, parentId: 1),
            CommentOnPost(3, 100, visibleAuthor)
        };

        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var controller = CreatePostsController(comments, viewerUserId: 10, visibility: visibility);

        var result = await controller.GetComments("100", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<CommentResponseDto>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("3", list[0].Id);
    }

    [Fact]
    public async Task CreateComment_ReturnsNotFound_WhenBlockedWithPostAuthor()
    {
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(10, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = CreatePostsController([], viewerUserId: 10, postAuthorId: 20, visibility: visibility);

        var result = await controller.CreateComment("100", new CreateCommentRequestDTO { Content = "Olá" }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateComment_ReturnsNotFound_WhenReplyingToBlockedAuthor()
    {
        var parent = CommentOnPost(1, 100, MinimalAuthor(5));
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(10, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(10, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetByIdNonDeletedWithAuthorAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);

        var controller = CreatePostsController([], viewerUserId: 10, postAuthorId: 20, commentsRepo: comments, visibility: visibility);

        var result = await controller.CreateComment(
            "100",
            new CreateCommentRequestDTO { Content = "reply", ParentCommentId = "1" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Like_ReturnsNotFound_WhenBlockedWithPostAuthor()
    {
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(10, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var likes = new Mock<ILikeRepository>();
        var notifications = new Mock<INotificationService>();
        var controller = CreatePostsController([], viewerUserId: 10, postAuthorId: 20, visibility: visibility, likes: likes, notifications: notifications);

        var result = await controller.Like("100", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        likes.Verify(x => x.TryAddPostLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        notifications.Verify(x => x.NotifyPostLikedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LikeComment_ReturnsNotFound_WhenBlockedWithCommentAuthor()
    {
        var comment = CommentOnPost(42, 100, MinimalAuthor(5));
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.AreUsersBlockedEitherWayAsync(10, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetByIdNonDeletedWithAuthorAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(comment);

        var likes = new Mock<ILikeRepository>();
        var controller = CreatePostsController([], viewerUserId: 10, postAuthorId: 20, commentsRepo: comments, visibility: visibility, likes: likes);

        var result = await controller.LikeComment("100", "42", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        likes.Verify(x => x.TryAddCommentLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFollowers_PassesHiddenIdsToRepository()
    {
        var hidden = new HashSet<int> { 5 };
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var follows = new Mock<IFollowRepository>();
        follows
            .Setup(x => x.ListFollowersPagedAsync(1, 1, 20, null, It.Is<IReadOnlyCollection<int>>(e => e.Contains(5)), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User> { MinimalAuthor(9) }, 1));

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdNoTrackingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MinimalAuthor(1));

        var controller = CreateUsersController(follows, users, visibility, viewerUserId: 10);

        var result = await controller.GetFollowers("1", cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CommunityMembers_ExcludesHiddenMembers()
    {
        var hidden = new HashSet<int> { 5 };
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var community = new Community { Id = 3, Name = "Test", Slug = "test", Visibility = "public" };
        var communities = new Mock<ICommunityRepository>();
        communities.Setup(x => x.GetByIdWithTagsNoTrackingAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(community);

        var memberships = new Mock<ICommunityMembershipRepository>();
        memberships
            .Setup(x => x.ListActiveMembersPagedOrderedAsync(
                3, 1, 20, It.Is<IReadOnlyCollection<int>>(e => e.Contains(5)), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<CommunityMembership>
            {
                new() { UserId = 9, Role = "member", User = MinimalAuthor(9) }
            }, 1));

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization.Setup(x => x.CanReadCommunityMembersAsync(community, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = new CommunitiesController(
            communities.Object,
            memberships.Object,
            Mock.Of<IJoinRequestRepository>(),
            Mock.Of<IPostRepository>(),
            Mock.Of<IPostEnrichmentService>(),
            Mock.Of<ICommunityPermissionService>(),
            Mock.Of<IUserEntitlementService>(),
            Mock.Of<ICommunitySubscriptionRepository>(),
            Mock.Of<ICommunityPremiumEntitlementService>(),
            Mock.Of<ICommunityDailyRollupRepository>(),
            Mock.Of<ICommunityDashboardAnalyticsService>(),
            Mock.Of<ICommunityPostBoostService>(),
            authorization.Object,
            Mock.Of<INotificationService>(),
            visibility.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "10")],
                        "Test"))
                }
            }
        };

        var result = await controller.Members("3", cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResponseDto<CommunityMemberItemDto>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal("9", payload.Items[0].User.Id);
    }

    [Fact]
    public async Task CommunityPosts_ExcludesHiddenAuthors()
    {
        var hidden = new HashSet<int> { 5 };
        var visibility = UserBlockTestHelpers.CreateVisibilityMock();
        visibility.Setup(v => v.GetHiddenUserIdsForViewerAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);

        var community = new Community { Id = 3, Name = "Test", Slug = "test", Visibility = "public" };
        var communities = new Mock<ICommunityRepository>();
        communities.Setup(x => x.GetByIdWithTagsNoTrackingAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(community);

        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.ListByCommunityIdPagedAsync(
                3, 1, 10, It.Is<IReadOnlyCollection<int>>(e => e.Contains(5)), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Post> { new() { Id = 2, UserId = 9, User = MinimalAuthor(9), Content = "x" } }, 1));

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment
            .Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Post> source, int? _, CancellationToken _) =>
                source.Select(p => new PostResponseDto { Id = p.Id.ToString() }).ToList());

        var controller = new CommunitiesController(
            communities.Object,
            Mock.Of<ICommunityMembershipRepository>(),
            Mock.Of<IJoinRequestRepository>(),
            posts.Object,
            enrichment.Object,
            Mock.Of<ICommunityPermissionService>(),
            Mock.Of<IUserEntitlementService>(),
            Mock.Of<ICommunitySubscriptionRepository>(),
            Mock.Of<ICommunityPremiumEntitlementService>(),
            Mock.Of<ICommunityDailyRollupRepository>(),
            Mock.Of<ICommunityDashboardAnalyticsService>(),
            Mock.Of<ICommunityPostBoostService>(),
            Mock.Of<IResourceAuthorizationService>(),
            Mock.Of<INotificationService>(),
            visibility.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "10")],
                        "Test"))
                }
            }
        };

        var result = await controller.CommunityPosts(3, cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResponseDto<PostResponseDto>>(ok.Value);
        Assert.Single(payload.Items);
    }

    private static User MinimalAuthor(int id) =>
        new()
        {
            Id = id,
            Username = $"u{id}",
            DisplayName = $"User {id}",
            Email = $"u{id}@example.com",
            Role = "User"
        };

    private static Comment CommentOnPost(int id, int postId, User author, int? parentId = null) =>
        new()
        {
            Id = id,
            PostId = postId,
            AuthorId = author.Id,
            Author = author,
            ParentCommentId = parentId,
            Content = "hello",
            CreatedAt = DateTime.UtcNow
        };

    private static PostsController CreatePostsController(
        IReadOnlyList<Comment> comments,
        int? viewerUserId,
        int postId = 100,
        int postAuthorId = 20,
        Mock<IUserRelationshipVisibilityService>? visibility = null,
        Mock<ICommentRepository>? commentsRepo = null,
        Mock<ILikeRepository>? likes = null,
        Mock<INotificationService>? notifications = null)
    {
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = postId, UserId = postAuthorId });

        var commentsMock = commentsRepo ?? new Mock<ICommentRepository>();
        if (commentsRepo == null)
        {
            commentsMock
                .Setup(x => x.ListActiveForPostWithAuthorAsync(postId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(comments.ToList());
        }

        var likesMock = likes ?? new Mock<ILikeRepository>();
        likesMock
            .Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());
        likesMock
            .Setup(x => x.GetCommentIdsLikedByUserAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), viewerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var httpContext = new DefaultHttpContext();
        if (viewerUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, viewerUserId.Value.ToString())],
                "Test"));
        }

        return new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            (likesMock).Object,
            commentsMock.Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            (notifications ?? new Mock<INotificationService>()).Object,
            (visibility ?? UserBlockTestHelpers.CreateVisibilityMock()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static UsersController CreateUsersController(
        Mock<IFollowRepository> follows,
        Mock<IUserRepository> users,
        Mock<IUserRelationshipVisibilityService> visibility,
        int viewerUserId)
    {
        var history = new Mock<IUsernameHistoryRepository>();
        history.Setup(x => x.AddAsync(It.IsAny<UsernameHistory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        history.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var stories = new Mock<IStoryRepository>();
        stories.Setup(x => x.GetUserIdsWithActiveStoriesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var badgeAward = new Mock<IBadgeAwardService>();
        badgeAward.Setup(x => x.GetUserBadgesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserBadgeDto>());

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
            badgeAward.Object,
            visibility.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, viewerUserId.ToString())],
                        "Test"))
                }
            }
        };
    }
}
