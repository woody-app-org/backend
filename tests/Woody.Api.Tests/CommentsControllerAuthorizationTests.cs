using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class CommentsControllerAuthorizationTests
{
    [Fact]
    public async Task Delete_ReturnsUnauthorized_ForAnonymousUser()
    {
        var controller = CreateController(actorUserId: null, authorizationAllows: false);

        var result = await controller.Delete("1", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsForbid_ForAuthenticatedUserWithoutPermission()
    {
        var controller = CreateController(actorUserId: 20, authorizationAllows: false);

        var result = await controller.Delete("1", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ForAuthorOrModerator()
    {
        var comments = new Mock<ICommentRepository>();
        comments
            .Setup(x => x.GetTrackedWithPostAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Comment { Id = 1, AuthorId = 10, Post = new Post { Id = 5, UserId = 10 } });
        comments
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanDeleteCommentAsync(It.IsAny<Comment>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(actorUserId: 10, comments, authorization);

        var result = await controller.Delete("1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        comments.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CommentsController CreateController(int? actorUserId, bool authorizationAllows)
    {
        var comments = new Mock<ICommentRepository>();
        comments
            .Setup(x => x.GetTrackedWithPostAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Comment { Id = 1, AuthorId = 10, Post = new Post { Id = 5, UserId = 10 } });
        comments
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanDeleteCommentAsync(It.IsAny<Comment>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorizationAllows);

        return CreateController(actorUserId, comments, authorization);
    }

    private static CommentsController CreateController(
        int? actorUserId,
        Mock<ICommentRepository> comments,
        Mock<IResourceAuthorizationService> authorization)
    {
        var controller = new CommentsController(comments.Object, authorization.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (actorUserId.HasValue)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, actorUserId.Value.ToString()) },
                    authenticationType: "Test"));
        }

        return controller;
    }
}
