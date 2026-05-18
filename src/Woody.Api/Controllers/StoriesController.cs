using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Stories;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api")]
public class StoriesController : ControllerBase
{
    private readonly IStoriesService _stories;

    public StoriesController(IStoriesService stories)
    {
        _stories = stories;
    }

    [HttpPost("stories")]
    [Authorize(Policy = "VerifiedAccount")]
    [EnableRateLimiting(RateLimitPolicyNames.ContentCreate)]
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStoryRequestDto body,
        CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _stories.CreateStoryAsync(me.Value, body, cancellationToken);
        return ToCreateActionResult(result);
    }

    [HttpGet("users/{userId:int}/stories")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.PublicRead)]
    [ProducesResponseType(typeof(IReadOnlyList<StoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoryDto>>> ListByUser(
        int userId,
        CancellationToken cancellationToken)
    {
        var viewerId = User.GetUserId();
        var stories = await _stories.GetActiveStoriesByUserAsync(userId, viewerId, cancellationToken);
        return Ok(stories);
    }

    [HttpDelete("stories/{storyId:int}")]
    [Authorize(Policy = "VerifiedAccount")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int storyId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _stories.DeleteStoryAsync(me.Value, storyId, cancellationToken);
        return ToMutationActionResult(result);
    }

    [HttpPost("stories/{storyId:int}/view")]
    [Authorize(Policy = "VerifiedAccount")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RegisterView(int storyId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _stories.RegisterViewAsync(me.Value, storyId, cancellationToken);
        return ToMutationActionResult(result);
    }

    [HttpGet("stories/{storyId:int}/views")]
    [Authorize(Policy = "VerifiedAccount")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    [ProducesResponseType(typeof(IReadOnlyList<StoryViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListViews(int storyId, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _stories.GetStoryViewsAsync(me.Value, storyId, cancellationToken);
        return result.Outcome switch
        {
            StoryOperationOutcome.Success => Ok(result.Views),
            StoryOperationOutcome.Forbidden => Forbid(),
            StoryOperationOutcome.NotFound => NotFound(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }

    private IActionResult ToCreateActionResult(StoryCommandResult result) =>
        result.Outcome switch
        {
            StoryOperationOutcome.Success => CreatedAtAction(
                nameof(ListByUser),
                new { userId = result.Story!.AuthorUserId },
                result.Story),
            StoryOperationOutcome.LimitReached => Conflict(new
            {
                error = result.Error,
                code = result.Code ?? StoryLimitReachedException.ErrorCode
            }),
            StoryOperationOutcome.InvalidMediaType => BadRequest(new { error = result.Error }),
            StoryOperationOutcome.InvalidContent => BadRequest(new { error = result.Error }),
            StoryOperationOutcome.InvalidUrl => BadRequest(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error ?? "Não foi possível criar o story." })
        };

    private IActionResult ToMutationActionResult(StoryCommandResult result) =>
        result.Outcome switch
        {
            StoryOperationOutcome.Success => NoContent(),
            StoryOperationOutcome.NotFound => NotFound(new { error = result.Error }),
            StoryOperationOutcome.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
}
