using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Domain.Entities;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ICommunityRepository _communities;
    private readonly IPostRepository _posts;
    private readonly IPostEnrichmentService _postEnrichment;
    private readonly IResourceAuthorizationService _authorization;

    public SearchController(
        IUserRepository users,
        ICommunityRepository communities,
        IPostRepository posts,
        IPostEnrichmentService postEnrichment,
        IResourceAuthorizationService authorization)
    {
        _users = users;
        _communities = communities;
        _posts = posts;
        _postEnrichment = postEnrichment;
        _authorization = authorization;
    }

    [AllowAnonymous]
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
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
            // Descoberta: privadas na pesquisa, sem interior na resposta (alinhado com listagem global).
            return Ok(new
            {
                communities = list
                    .Select(c => EntityMappers.ToCommunityDto(c, viewerSeesPrivateInterior: false))
                    .ToList()
            });
        }

        var posts = await _posts.SearchNonDeletedWithNavAsync(n, 200, cancellationToken);
        var visiblePosts = new List<Post>();
        foreach (var post in posts)
        {
            if (await _authorization.CanReadPostAsync(post, viewerId, cancellationToken))
                visiblePosts.Add(post);
            if (visiblePosts.Count >= 80)
                break;
        }

        var dtos = await _postEnrichment.ToPostDtosAsync(visiblePosts, viewerId, cancellationToken);
        return Ok(new { posts = dtos });
    }
}
