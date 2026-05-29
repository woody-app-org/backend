using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Tests;

/// <summary>
/// Testes de segurança IDOR — verifica que substituir IDs/publicIds/slugs não dá acesso indevido.
/// Cobre: posts (editar/apagar/ler/curtir alheio), comentários ocultos, stories.
/// </summary>
public class IborSecurityTests
{
    // -------------------------------------------------------------------------
    // Posts — editar/apagar alheio
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Post_ReturnsForbid_WhenViewerIsNotAuthor()
    {
        var post = SampleProfilePost(id: 10, authorId: 1);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdTrackedWithTagsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanEditPostAsync(post, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = CreatePostsController(posts, authorization: auth, userId: 99);

        var result = await controller.Update("10", new UpdatePostRequestDTO { Content = "hacked" }, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Delete_Post_ReturnsForbid_WhenViewerIsNotAuthor()
    {
        var post = SampleProfilePost(id: 11, authorId: 1);
        var posts = new Mock<IPostRepository>();
        // Delete usa GetByIdTrackedAsync (não o de nav), logo este é o mock correto.
        posts.Setup(x => x.GetByIdTrackedAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        posts.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanDeletePostAsync(post, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = CreatePostsController(posts, authorization: auth, userId: 99);

        var result = await controller.Delete("11", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    // -------------------------------------------------------------------------
    // Posts — ler post de comunidade privada sem membership
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByPublicId_PrivateCommunityPost_ReturnsNotFound_ForNonMember()
    {
        var post = SampleCommunityPost(id: 20, publicId: "pst_priv000001", communityVisibility: "private");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByPublicIdNonDeletedWithNavAsync("pst_priv000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 50, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var enrichment = new Mock<IPostEnrichmentService>();
        var controller = CreatePostsController(posts, authorization: auth, enrichment: enrichment, userId: 50);

        var result = await controller.GetByPublicId("pst_priv000001", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByPublicId_PrivateCommunityPost_ReturnsNotFound_ForAnonymous()
    {
        var post = SampleCommunityPost(id: 21, publicId: "pst_priv000002", communityVisibility: "private");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByPublicIdNonDeletedWithNavAsync("pst_priv000002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var enrichment = new Mock<IPostEnrichmentService>();
        var controller = CreatePostsController(posts, authorization: auth, enrichment: enrichment, userId: null);

        var result = await controller.GetByPublicId("pst_priv000002", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByPublicId_UnknownPublicId_ReturnsNotFound()
    {
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByPublicIdNonDeletedWithNavAsync("pst_unknown0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        var controller = CreatePostsController(posts, userId: null);

        var result = await controller.GetByPublicId("pst_unknown0001", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // Posts — like/comentário em post de comunidade privada
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Like_PrivateCommunityPost_ReturnsNotFound_ForNonMember()
    {
        var post = SampleCommunityPost(id: 30, publicId: "pst_priv000003", communityVisibility: "private");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 55, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = CreatePostsController(posts, authorization: auth, userId: 55);

        var result = await controller.Like("30", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateComment_PrivateCommunityPost_ReturnsNotFound_ForNonMember()
    {
        var post = SampleCommunityPost(id: 31, publicId: "pst_priv000004", communityVisibility: "private");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(31, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 55, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = CreatePostsController(posts, authorization: auth, userId: 55);

        var result = await controller.CreateComment("31", new CreateCommentRequestDTO { Content = "spy" }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // Posts — like em comentário oculto (non-mod deve receber 404)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LikeComment_HiddenComment_ReturnsNotFound_ForNonModerator()
    {
        var post = SampleProfilePost(id: 5, authorId: 10); // author = 10
        var hiddenComment = new Comment
        {
            Id = 42,
            PostId = 5,
            AuthorId = 7,
            HiddenByPostAuthorAt = DateTime.UtcNow,
            Content = "oculto",
            CreatedAt = DateTime.UtcNow
        };

        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetByIdNonDeletedWithAuthorAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(hiddenComment);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanModeratePostCommentsAsync(post, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var likes = new Mock<ILikeRepository>();

        var controller = CreatePostsController(posts, comments: comments, likes: likes, authorization: auth, userId: 20);

        var result = await controller.LikeComment("5", "42", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        likes.Verify(x => x.TryAddCommentLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LikeComment_HiddenComment_ReturnsOk_ForModerator()
    {
        var post = SampleProfilePost(id: 6, authorId: 10);
        var hiddenComment = new Comment
        {
            Id = 43,
            PostId = 6,
            AuthorId = 7,
            HiddenByPostAuthorAt = DateTime.UtcNow,
            Content = "oculto",
            CreatedAt = DateTime.UtcNow
        };

        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetByIdNonDeletedWithAuthorAsync(43, It.IsAny<CancellationToken>())).ReturnsAsync(hiddenComment);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // post author (10) → CanModerate returns true
        auth.Setup(x => x.CanModeratePostCommentsAsync(post, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var likes = new Mock<ILikeRepository>();
        likes.Setup(x => x.TryAddCommentLikeAsync(10, 43, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        likes.Setup(x => x.GetCommentLikeCountsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [43] = 1 });
        likes.Setup(x => x.GetCommentIdsLikedByUserAsync(10, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { 43 });

        var controller = CreatePostsController(posts, comments: comments, likes: likes, authorization: auth, userId: 10);

        var result = await controller.LikeComment("6", "43", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UnlikeComment_HiddenComment_ReturnsNotFound_ForNonModerator()
    {
        var post = SampleProfilePost(id: 7, authorId: 10);
        var hiddenComment = new Comment
        {
            Id = 44,
            PostId = 7,
            AuthorId = 7,
            HiddenByPostAuthorAt = DateTime.UtcNow,
            Content = "oculto",
            CreatedAt = DateTime.UtcNow
        };

        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetByIdNonDeletedWithAuthorAsync(44, It.IsAny<CancellationToken>())).ReturnsAsync(hiddenComment);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        auth.Setup(x => x.CanModeratePostCommentsAsync(post, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var likes = new Mock<ILikeRepository>();

        var controller = CreatePostsController(posts, comments: comments, likes: likes, authorization: auth, userId: 20);

        var result = await controller.UnlikeComment("7", "44", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        likes.Verify(x => x.RemoveCommentLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Posts — comentário com postId de outra comunidade (cross-post IDOR)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LikeComment_CommentBelongsToOtherPost_ReturnsNotFound()
    {
        var post = SampleProfilePost(id: 5, authorId: 10);
        var commentOnOtherPost = new Comment
        {
            Id = 99,
            PostId = 999, // pertence a outro post
            AuthorId = 7,
            Content = "outro post",
            CreatedAt = DateTime.UtcNow
        };

        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var comments = new Mock<ICommentRepository>();
        comments.Setup(x => x.GetByIdNonDeletedWithAuthorAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(commentOnOtherPost);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = CreatePostsController(posts, comments: comments, authorization: auth, userId: 20);

        // comment.PostId = 999 != 5 → NotFound
        var result = await controller.LikeComment("5", "99", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Post SampleProfilePost(int id, int authorId)
    {
        var user = new User
        {
            Id = authorId,
            Username = $"user{authorId}",
            DisplayName = "Test",
            Email = $"u{authorId}@test.com",
            Role = "User"
        };
        return new Post
        {
            Id = id,
            PublicId = $"pst_test{id:D6}",
            UserId = authorId,
            User = user,
            Content = "test",
            PublicationContext = PostPublicationContext.Profile,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Post SampleCommunityPost(int id, string publicId, string communityVisibility)
    {
        var author = new User
        {
            Id = 1,
            Username = "author",
            DisplayName = "Author",
            Email = "author@test.com",
            Role = "User"
        };
        var community = new Community
        {
            Id = 10,
            Slug = "test-community",
            Name = "Test",
            Description = "desc",
            Category = "outro",
            Rules = string.Empty,
            Visibility = communityVisibility,
            OwnerUserId = 1,
            MemberCount = 1
        };
        return new Post
        {
            Id = id,
            PublicId = publicId,
            UserId = author.Id,
            User = author,
            Content = "private content",
            PublicationContext = PostPublicationContext.Community,
            CommunityId = community.Id,
            Community = community,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PostsController CreatePostsController(
        Mock<IPostRepository>? posts = null,
        Mock<ICommentRepository>? comments = null,
        Mock<ILikeRepository>? likes = null,
        Mock<IPostEnrichmentService>? enrichment = null,
        Mock<IResourceAuthorizationService>? authorization = null,
        int? userId = null)
    {
        posts ??= new Mock<IPostRepository>();
        authorization ??= new Mock<IResourceAuthorizationService>();
        comments ??= new Mock<ICommentRepository>();
        likes ??= new Mock<ILikeRepository>();
        enrichment ??= new Mock<IPostEnrichmentService>();

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            comments.Object,
            enrichment.Object,
            new Mock<IContentPinningService>().Object,
            authorization.Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object);

        SetUser(controller, userId);
        return controller;
    }

    private static void SetUser(ControllerBase controller, int? userId)
    {
        var identity = userId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
