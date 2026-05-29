using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Tests;

public class PostsByPublicIdControllerTests
{
    [Fact]
    public async Task GetByPublicId_ReturnsPost_WhenAuthorized()
    {
        var post = SamplePost(7, "pst_abc123xyz456");
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByPublicIdNonDeletedWithNavAsync("pst_abc123xyz456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var enrichment = new Mock<IPostEnrichmentService>();
        enrichment.Setup(x => x.ToPostDtosAsync(It.IsAny<IReadOnlyList<Post>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostResponseDto>
            {
                new() { Id = "7", PublicId = "pst_abc123xyz456", Content = "hello" }
            });

        var controller = CreateController(posts, auth, enrichment);

        var result = await controller.GetByPublicId("pst_abc123xyz456", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PostResponseDto>(ok.Value);
        Assert.Equal("pst_abc123xyz456", dto.PublicId);
    }

    [Fact]
    public async Task GetByPublicId_ReturnsNotFound_WhenPrivateAndUnauthorized()
    {
        var post = SamplePost(8, "pst_private00001", communityId: 3);
        var posts = new Mock<IPostRepository>();
        posts.Setup(x => x.GetByPublicIdNonDeletedWithNavAsync("pst_private00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var auth = new Mock<IResourceAuthorizationService>();
        auth.Setup(x => x.CanReadPostAsync(post, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = CreateController(posts, auth, new Mock<IPostEnrichmentService>());

        var result = await controller.GetByPublicId("pst_private00001", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static Post SamplePost(int id, string publicId, int? communityId = null)
    {
        var user = new User
        {
            Id = 1,
            Username = "ana",
            DisplayName = "Ana",
            Email = "a@example.com",
            Role = "User"
        };
        return new Post
        {
            Id = id,
            PublicId = publicId,
            UserId = user.Id,
            User = user,
            Content = "hello",
            CreatedAt = DateTime.UtcNow,
            PublicationContext = communityId.HasValue ? PostPublicationContext.Community : PostPublicationContext.Profile,
            CommunityId = communityId,
            Community = communityId.HasValue
                ? new Community
                {
                    Id = communityId.Value,
                    Slug = "club",
                    Name = "Club",
                    Visibility = "private",
                    OwnerUserId = 1,
                    MemberCount = 1,
                    Category = "general",
                    Description = string.Empty,
                    Rules = string.Empty
                }
                : null
        };
    }

    private static PostsController CreateController(
        Mock<IPostRepository> posts,
        Mock<IResourceAuthorizationService> auth,
        Mock<IPostEnrichmentService> enrichment)
    {
        return new PostsController(
            posts.Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            new Mock<ILikeRepository>().Object,
            new Mock<ICommentRepository>().Object,
            enrichment.Object,
            new Mock<IContentPinningService>().Object,
            auth.Object,
            new Mock<INotificationService>().Object,
            UserBlockTestHelpers.CreateVisibilityMock().Object, new Mock<IPostSharingService>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
