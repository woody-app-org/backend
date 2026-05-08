using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/feed")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feed;

    public FeedController(IFeedService feed)
    {
        _feed = feed;
    }

    [Authorize(Policy = "VerifiedAccount")]
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<PaginatedResponseDto<PostResponseDto>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string filter = "trending",
        CancellationToken cancellationToken = default)
    {
        var viewerId = User.GetUserId();
        var result = await _feed.GetFeedAsync(page, pageSize, filter, viewerId, cancellationToken);
        return Ok(result);
    }
}
