using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public sealed class PostsControllerCreateCommentGifTests
{
    private sealed class CommentCapture
    {
        public Comment?[] Items { get; } = new Comment?[1];
    }

    private const string ValidHttpsGif =
        "https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif";

    private static User MinimalAuthor(int id, string username = "u1") => new()
    {
        Id = id,
        Username = username,
        DisplayName = "Author",
        Email = $"{username}@t.example",
        Role = "User",
    };

    private static PostsController CreateController(
        bool canRead,
        int userId,
        Comment? parentForReply,
        CommentCapture capture,
        out Mock<ICommentRepository> comments)
    {
        const int postId = 5;
        var post = new Post { Id = postId, UserId = 20 };

        var posts = new Mock<IPostRepository>();
        posts
            .Setup(p => p.GetByIdNonDeletedForCommentLookupAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        comments = new Mock<ICommentRepository>();
        comments
            .Setup(c => c.Add(It.IsAny<Comment>()))
            .Callback<Comment>(x =>
            {
                x.Id = 99;
                capture.Items[0] = x;
            });
        comments.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        comments
            .Setup(c => c.GetByIdNonDeletedWithAuthorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                if (parentForReply != null && id == parentForReply.Id)
                    return parentForReply;

                var cap = capture.Items[0];
                if (cap != null && id == cap.Id)
                {
                    cap.Author = MinimalAuthor(userId, "me");
                    return cap;
                }

                return null;
            });

        var likes = new Mock<ILikeRepository>();
        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(a => a.CanReadPostAsync(It.IsAny<Post>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canRead);

        var notifications = new Mock<INotificationService>();
        notifications
            .Setup(n => n.NotifyPostCommentAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifications
            .Setup(n => n.NotifyCommentReplyAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            comments.Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            notifications.Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                            authenticationType: "Test")),
                },
            },
        };

        return controller;
    }

    [Fact]
    public async Task CreateComment_EmptyWithoutGif_ReturnsBadRequest()
    {
        var capture = new CommentCapture();
        var controller = CreateController(canRead: true, userId: 10, parentForReply: null, capture, out _);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO { Content = "   " },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateComment_TextOnly_DoesNotSetGif()
    {
        var capture = new CommentCapture();
        var controller = CreateController(canRead: true, userId: 10, parentForReply: null, capture, out _);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO { Content = "  Olá mundo  " },
            CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentResponseDto>>(result);
        var dto = Assert.IsType<CommentResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Equal("Olá mundo", dto.Content);
        Assert.Null(dto.Gif);

        var saved = capture.Items[0]!;
        Assert.Equal("Olá mundo", saved.Content);
        Assert.Null(saved.GifUrl);
    }

    [Fact]
    public async Task CreateComment_GifOnly_Allows_empty_text()
    {
        var capture = new CommentCapture();
        var controller = CreateController(canRead: true, userId: 10, parentForReply: null, capture, out _);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO
            {
                Content = null,
                GifUrl = ValidHttpsGif,
                GifProvider = "local_catalog",
                GifExternalId = "wm-earth",
                GifTitle = "Terra",
            },
            CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentResponseDto>>(result);
        var dto = Assert.IsType<CommentResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Empty(dto.Content);
        Assert.NotNull(dto.Gif);
        Assert.Equal(ValidHttpsGif, dto.Gif!.Url);

        var saved = capture.Items[0]!;
        Assert.Equal(string.Empty, saved.Content);
        Assert.Equal(ValidHttpsGif, saved.GifUrl);
        Assert.Equal("local_catalog", saved.GifProvider);
        Assert.Equal("wm-earth", saved.GifExternalId);
    }

    [Fact]
    public async Task CreateComment_TextAndGif_Persists_both()
    {
        var capture = new CommentCapture();
        var controller = CreateController(canRead: true, userId: 10, parentForReply: null, capture, out _);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO
            {
                Content = "Vejam",
                GifUrl = ValidHttpsGif,
                GifProvider = "klipy",
                GifExternalId = "abc-1",
            },
            CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentResponseDto>>(result);
        var dto = Assert.IsType<CommentResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);
        Assert.Equal("Vejam", dto.Content);
        Assert.NotNull(dto.Gif);

        var saved = capture.Items[0]!;
        Assert.Equal("Vejam", saved.Content);
        Assert.Equal(ValidHttpsGif, saved.GifUrl);
        Assert.Equal("klipy", saved.GifProvider);
    }

    [Fact]
    public async Task CreateComment_InvalidGifUrl_ReturnsBadRequest()
    {
        var capture = new CommentCapture();
        var controller = CreateController(canRead: true, userId: 10, parentForReply: null, capture, out _);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO
            {
                Content = "x",
                GifUrl = "javascript:alert(1)",
                GifProvider = "local_catalog",
                GifExternalId = "x",
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null(capture.Items[0]);
    }

    [Fact]
    public async Task CreateComment_NoReadAccess_ReturnsNotFound()
    {
        var capture = new CommentCapture();
        var controller = CreateController(canRead: false, userId: 10, parentForReply: null, capture, out _);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO { Content = "hi" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Null(capture.Items[0]);
    }

    [Fact]
    public async Task CreateComment_WithParent_sets_parent_id()
    {
        var capture = new CommentCapture();
        var parent = new Comment
        {
            Id = 50,
            PostId = 5,
            AuthorId = 7,
            Content = "root",
            CreatedAt = DateTime.UtcNow,
            Author = MinimalAuthor(7, "parent"),
        };

        var controller = CreateController(canRead: true, userId: 10, parentForReply: parent, capture, out var comments);

        var result = await controller.CreateComment(
            "5",
            new CreateCommentRequestDTO { Content = "resposta", ParentCommentId = "50" },
            CancellationToken.None);

        var ok = Assert.IsType<ActionResult<CommentResponseDto>>(result);
        Assert.IsType<CommentResponseDto>(Assert.IsType<OkObjectResult>(ok.Result!).Value);

        var saved = capture.Items[0]!;
        Assert.Equal(50, saved.ParentCommentId);
        comments.Verify(c => c.Add(It.Is<Comment>(x => x.ParentCommentId == 50)), Times.Once);
    }
}
