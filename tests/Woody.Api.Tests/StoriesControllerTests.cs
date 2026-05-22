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

    // -------------------------------------------------------------------------
    // IDOR: delete story alheio → 404 (anti-enumeração, não revela existência)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_OtherUsersStory_ReturnsNotFound_NotForbid()
    {
        // O serviço retorna NotFound quando o actor não é o autor (anti-enumeração).
        var stories = new Mock<IStoriesService>();
        stories.Setup(s => s.DeleteStoryAsync(99, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryCommandResult(StoryOperationOutcome.NotFound, null, "Story não encontrado."));

        var controller = CreateController(stories, userId: 99);
        var result = await controller.Delete(1, default);

        // Deve ser 404, nunca 403, para não confirmar que o story existe.
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // -------------------------------------------------------------------------
    // IDOR: ListViews de story alheio → 404 (após correção da fase 4)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ListViews_OtherUsersStory_ReturnsNotFound_NotForbid()
    {
        var stories = new Mock<IStoriesService>();
        stories.Setup(s => s.GetStoryViewsAsync(99, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryViewsCommandResult(StoryOperationOutcome.Forbidden, null, "Não autorizado."));

        var controller = CreateController(stories, userId: 99);
        var result = await controller.ListViews(1, default);

        // Forbidden é mapeado para NotFound no controller (anti-enumeração).
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ListViews_OwnStory_ReturnsOk()
    {
        var stories = new Mock<IStoriesService>();
        stories.Setup(s => s.GetStoryViewsAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoryViewsCommandResult(StoryOperationOutcome.Success, new List<StoryViewDto>()));

        var controller = CreateController(stories, userId: 1);
        var result = await controller.ListViews(5, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
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
