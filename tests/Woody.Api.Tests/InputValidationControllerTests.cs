using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Validation;
using Woody.Domain.Entities;

namespace Woody.Api.Tests;

public class InputValidationControllerTests
{
    [Fact]
    public async Task CreatePost_RejectsNonHttpsImageUrl()
    {
        var controller = CreatePostsController();

        var result = await controller.Create(new CreatePostRequestDTO
        {
            Title = "Título seguro",
            Content = "Conteúdo seguro",
            ImageUrl = "http://tracker.example/pixel.png"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePost_RejectsOversizedContentBeforePersistence()
    {
        var posts = new Mock<IPostRepository>();
        var controller = CreatePostsController(posts: posts);

        var result = await controller.Create(new CreatePostRequestDTO
        {
            Title = "Título seguro",
            Content = new string('x', InputValidationLimits.PostContentMaxLength + 1)
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        posts.Verify(x => x.Add(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task CreateReport_RejectsOversizedDetails()
    {
        var controller = CreateReportsController();

        var result = await controller.Create(new ReportRequestDTO
        {
            TargetType = "post",
            PostId = "1",
            ReasonCode = "spam",
            Details = new string('x', InputValidationLimits.ReportDetailsMaxLength + 1)
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PatchProfile_RejectsNonHttpsAvatarUrlBeforeSaving()
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdTrackedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 10,
                Username = "ana",
                Email = "ana@example.com",
                Role = "User",
                DisplayName = "Ana"
            });

        var controller = CreateUsersController(users);

        var result = await controller.PatchMe(new UpdateProfileRequestDTO
        {
            Name = "Ana",
            Username = "ana",
            AvatarUrl = "javascript:alert(1)"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        users.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static PostsController CreatePostsController(Mock<IPostRepository>? posts = null)
    {
        var controller = new PostsController(
            (posts ?? new Mock<IPostRepository>()).Object,
            new Mock<ICommunityRepository>().Object,
            new Mock<ICommunityPermissionService>().Object,
            new Mock<ILikeRepository>().Object,
            new Mock<ICommentRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IContentPinningService>().Object,
            new Mock<IResourceAuthorizationService>().Object,
            new Mock<IUserNotificationService>().Object);
        SetUser(controller);
        return controller;
    }

    private static ReportsController CreateReportsController()
    {
        var controller = new ReportsController(
            new Mock<IContentReportRepository>().Object,
            new Mock<IPostRepository>().Object,
            new Mock<ICommentRepository>().Object,
            new Mock<IResourceAuthorizationService>().Object);
        SetUser(controller);
        return controller;
    }

    private static UsersController CreateUsersController(Mock<IUserRepository> users)
    {
        var controller = new UsersController(
            users.Object,
            new Mock<ICommunityMembershipRepository>().Object,
            new Mock<IFollowRepository>().Object,
            new Mock<IPostRepository>().Object,
            new Mock<IPostEnrichmentService>().Object,
            new Mock<IUserNotificationService>().Object);
        SetUser(controller);
        return controller;
    }

    private static void SetUser(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "10") },
                    "Test"))
            }
        };
    }
}
