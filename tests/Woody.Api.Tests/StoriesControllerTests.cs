using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Stories;

namespace Woody.Api.Tests;

public class StoriesControllerTests
{
    [Fact]
    public async Task Create_LimitReached_ReturnsConflictWithCode()
    {
        var stories = new Mock<IStoriesService>();
        stories.Setup(s => s.CreateStoryAsync(1, It.IsAny<CreateStoryRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryCommandResult(
                StoryOperationOutcome.LimitReached,
                null,
                "limite",
                StoryLimitReachedException.ErrorCode));

        var controller = CreateController(stories, userId: 1);
        var result = await controller.Create(new CreateStoryRequestDto { MediaType = "text", Text = "x" }, default);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var stories = new Mock<IStoriesService>();
        stories.Setup(s => s.DeleteStoryAsync(1, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryCommandResult(StoryOperationOutcome.NotFound, null, "Story não encontrado."));

        var controller = CreateController(stories, userId: 1);
        var result = await controller.Delete(99, default);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static StoriesController CreateController(Mock<IStoriesService> stories, int userId)
    {
        var controller = new StoriesController(stories.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "Test"))
                }
            }
        };
        return controller;
    }
}
