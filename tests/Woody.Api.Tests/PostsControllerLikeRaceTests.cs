using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class PostsControllerLikeRaceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Like_ReturnsNoContent_ForNewOrAlreadyExistingLike(bool inserted)
    {
        var likes = new Mock<ILikeRepository>();
        likes
            .Setup(x => x.TryAddPostLikeAsync(10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inserted);
        var controller = CreateController(likes);

        var result = await controller.Like("5", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        likes.Verify(x => x.TryAddPostLikeAsync(10, 5, It.IsAny<CancellationToken>()), Times.Once);
        likes.Verify(x => x.ExistsPostLikeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PostsController CreateController(Mock<ILikeRepository> likes)
    {
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByIdNonDeletedForCommentLookupAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Post { Id = 5, UserId = 20 });

        var authorization = new Mock<IResourceAuthorizationService>();
        authorization
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            likes.Object,
            new Mock<ICommentRepository>().Object,
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

        return controller;
    }
}
