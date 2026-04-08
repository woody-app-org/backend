using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDto<PostResponseDto>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string filter = "trending",
        CancellationToken cancellationToken = default)
    {
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var result = await _feed.GetFeedAsync(page, pageSize, filter, viewerId, cancellationToken);
        return Ok(result);
    }
}
