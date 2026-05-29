using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public sealed class PostsControllerCommentLikeTests
{
    private static User MinimalAuthor(int id, string username = "u1") => new()
    {
        Id = id,
        Username = username,
        DisplayName = "Author",
        Email = $"{username}@t.example",
        Role = "User"
    };

    private static Comment CommentOnPost(int id, int postId, User author) => new()
    {
        Id = id,
        PostId = postId,
        AuthorId = author.Id,
        Author = author,
        Content = "hello",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task LikeComment_ReturnsOk_WithCountsFromRepository()
    {
        var author = MinimalAuthor(7);
        var comment = CommentOnPost(42, 5, author);
        var (controller, likes) = CreateController(comment, postId: 5, userId: 10, canRead: true);
        likes
            .Setup(x => x.TryAddCommentLikeAsync(10, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        likes
            .Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [42] = 3 });
        likes
            .Setup(x => x.GetCommentIdsLikedByUserAsync(10, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { 42 });

        var result = await controller.LikeComment("5", "42", CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentLikeMutationResponseDto>>(result);
        var body = Assert.IsType<CommentLikeMutationResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Equal(3, body.LikesCount);
        Assert.True(body.LikedByCurrentUser);
        likes.Verify(x => x.TryAddCommentLikeAsync(10, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LikeComment_SecondInsertDuplicate_StillReturnsOkAndLiked()
    {
        var author = MinimalAuthor(7);
        var comment = CommentOnPost(42, 5, author);
        var (controller, likes) = CreateController(comment, postId: 5, userId: 10, canRead: true);
        likes
            .Setup(x => x.TryAddCommentLikeAsync(10, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        likes
            .Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [42] = 1 });
        likes
            .Setup(x => x.GetCommentIdsLikedByUserAsync(10, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { 42 });

        var result = await controller.LikeComment("5", "42", CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentLikeMutationResponseDto>>(result);
        var body = Assert.IsType<CommentLikeMutationResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Equal(1, body.LikesCount);
        Assert.True(body.LikedByCurrentUser);
    }

    [Fact]
    public async Task UnlikeComment_ReturnsOk_AndLikedFalse()
    {
        var author = MinimalAuthor(7);
        var comment = CommentOnPost(42, 5, author);
        var (controller, likes) = CreateController(comment, postId: 5, userId: 10, canRead: true);
        likes
            .Setup(x => x.RemoveCommentLikeAsync(10, 42, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        likes
            .Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await controller.UnlikeComment("5", "42", CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentLikeMutationResponseDto>>(result);
        var body = Assert.IsType<CommentLikeMutationResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Equal(0, body.LikesCount);
        Assert.False(body.LikedByCurrentUser);
        likes.Verify(x => x.RemoveCommentLikeAsync(10, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LikeComment_NoReadAccess_ReturnsNotFound()
    {
        var author = MinimalAuthor(7);
        var comment = CommentOnPost(42, 5, author);
        var (controller, likes) = CreateController(comment, postId: 5, userId: 10, canRead: false);

        var result = await controller.LikeComment("5", "42", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        likes.Verify(x => x.TryAddCommentLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LikeComment_WrongPost_ReturnsNotFound()
    {
        var author = MinimalAuthor(7);
        var comment = CommentOnPost(42, 99, author);
        var (controller, likes) = CreateController(comment, postId: 5, userId: 10, canRead: true);

        var result = await controller.LikeComment("5", "42", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        likes.Verify(x => x.TryAddCommentLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LikeComment_DeletedComment_ReturnsNotFound()
    {
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = 5, UserId = 20 });

        var comments = new Mock<ICommentRepository>();
        comments
            .Setup(x => x.GetByIdNonDeletedWithAuthorAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        var likes = new Mock<ILikeRepository>();
        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            comments.Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "10") },
                        "Test"))
                }
            }
        };

        var result = await controller.LikeComment("5", "42", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        likes.Verify(x => x.TryAddCommentLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComments_EnrichesLikes_AndSkipsViewerLikesWhenAnonymous()
    {
        var author = MinimalAuthor(7);
        var c1 = CommentOnPost(10, 5, author);
        var c2 = CommentOnPost(11, 5, author);
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = 5, UserId = 20 });

        var comments = new Mock<ICommentRepository>();
        comments
            .Setup(x => x.ListActiveForPostWithAuthorAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Comment> { c1, c2 });

        var likes = new Mock<ILikeRepository>();
        likes
            .Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 2, [11] = 5 });

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            comments.Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.GetComments("5", CancellationToken.None);

        var ok = Assert.IsType<ActionResult<List<CommentResponseDto>>>(result);
        var list = Assert.IsType<List<CommentResponseDto>>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Equal(2, list.Count);
        Assert.Equal(2, list[0].LikesCount);
        Assert.False(list[0].LikedByCurrentUser);
        Assert.Equal(5, list[1].LikesCount);
        Assert.False(list[1].LikedByCurrentUser);
        likes.Verify(x => x.GetCommentIdsLikedByUserAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComments_Authenticated_CallsViewerLikedBatch()
    {
        var author = MinimalAuthor(7);
        var c1 = CommentOnPost(10, 5, author);
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = 5, UserId = 20 });

        var comments = new Mock<ICommentRepository>();
        comments
            .Setup(x => x.ListActiveForPostWithAuthorAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Comment> { c1 });

        var likes = new Mock<ILikeRepository>();
        likes
            .Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });
        likes
            .Setup(x => x.GetCommentIdsLikedByUserAsync(10, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            comments.Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "10") },
                        "Test"))
                }
            }
        };

        var result = await controller.GetComments("5", CancellationToken.None);

        var ok = Assert.IsType<ActionResult<List<CommentResponseDto>>>(result);
        var list = Assert.IsType<List<CommentResponseDto>>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Single(list);
        Assert.Equal(1, list[0].LikesCount);
        Assert.False(list[0].LikedByCurrentUser);
        likes.Verify(x => x.GetCommentIdsLikedByUserAsync(10, It.Is<IReadOnlyList<int>>(ids => ids.Count == 1 && ids[0] == 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (PostsController Controller, Mock<ILikeRepository> Likes) CreateController(
        Comment comment,
        int postId,
        int userId,
        bool canRead)
    {
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = postId, UserId = 20 });

        var comments = new Mock<ICommentRepository>();
        comments
            .Setup(x => x.GetByIdNonDeletedWithAuthorAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        var likes = new Mock<ILikeRepository>();
        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canRead);

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            comments.Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                        "Test"))
                }
            }
        };

        return (controller, likes);
    }
}
