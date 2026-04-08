using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ICommunityRepository _communities;
    private readonly IPostRepository _posts;
    private readonly IPostEnrichmentService _postEnrichment;

    public SearchController(
        IUserRepository users,
        ICommunityRepository communities,
        IPostRepository posts,
        IPostEnrichmentService postEnrichment)
    {
        _users = users;
        _communities = communities;
        _posts = posts;
        _postEnrichment = postEnrichment;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string mode = "posts",
        CancellationToken cancellationToken = default)
    {
        var query = (q ?? string.Empty).Trim();
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        if (string.IsNullOrEmpty(query))
        {
            if (string.Equals(mode, "people", StringComparison.OrdinalIgnoreCase))
                return Ok(new { people = new List<UserPublicDto>() });
            if (string.Equals(mode, "communities", StringComparison.OrdinalIgnoreCase))
                return Ok(new { communities = new List<CommunityResponseDto>() });
            return Ok(new { posts = new List<PostResponseDto>() });
        }

        var n = query.ToLowerInvariant();

        if (string.Equals(mode, "people", StringComparison.OrdinalIgnoreCase))
        {
            var users = await _users.SearchUsersNoTrackingAsync(n, 50, cancellationToken);
            return Ok(new { people = users.Select(EntityMappers.ToUserPublicDto).ToList() });
        }

        if (string.Equals(mode, "communities", StringComparison.OrdinalIgnoreCase))
        {
            var list = await _communities.SearchWithTagsAsync(n, 50, cancellationToken);
            return Ok(new { communities = list.Select(EntityMappers.ToCommunityDto).ToList() });
        }

        var posts = await _posts.SearchNonDeletedWithNavAsync(n, 80, cancellationToken);
        var dtos = await _postEnrichment.ToPostDtosAsync(posts, viewerId, cancellationToken);
        return Ok(new { posts = dtos });
    }
}
