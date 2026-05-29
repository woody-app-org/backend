using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public sealed class PostsControllerPostValidationTests
{
    [Fact]
    public async Task Create_rejects_empty_content_when_no_media()
    {
        var posts = new Mock<IPostRepository>();
        var controller = CreateController(posts);

        var result = await controller.Create(
            new CreatePostRequestDTO { Content = "" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        posts.Verify(p => p.Add(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task Create_rejects_four_hashtags()
    {
        var posts = new Mock<IPostRepository>();
        var controller = CreateController(posts);

        var result = await controller.Create(
            new CreatePostRequestDTO
            {
                Content = "Olá",
                Tags = new List<string> { "a", "b", "c", "d" },
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        posts.Verify(p => p.Add(It.IsAny<Post>()), Times.Never);
    }

    private static PostsController CreateController(Mock<IPostRepository> posts)
    {
        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            new Mock<ILikeRepository>().Object,
            new Mock<ICommentRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            new Mock<IResourceAuthorizationService>().Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "10") },
                    "Test")),
            },
        };
        return controller;
    }
}
