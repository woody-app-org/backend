using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Infrastructure.Mapping;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public SearchController(WoodyDbContext db)
    {
        _db = db;
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
            var users = await _db.Users.AsNoTracking()
                .Where(u => u.Username.ToLower().Contains(n)
                            || (u.DisplayName != null && u.DisplayName.ToLower().Contains(n))
                            || u.Email.ToLower().Contains(n))
                .Take(50)
                .ToListAsync(cancellationToken);
            return Ok(new { people = users.Select(EntityMappers.ToUserPublicDto).ToList() });
        }

        if (string.Equals(mode, "communities", StringComparison.OrdinalIgnoreCase))
        {
            var list = await _db.Communities.AsNoTracking()
                .Include(c => c.Tags)
                .Where(c => c.Name.ToLower().Contains(n) || c.Slug.ToLower().Contains(n) || c.Description.ToLower().Contains(n))
                .Take(50)
                .ToListAsync(cancellationToken);
            return Ok(new { communities = list.Select(EntityMappers.ToCommunityDto).ToList() });
        }

        var posts = await _db.Posts.AsNoTracking()
            .Where(p => p.DeletedAt == null && (p.Title.ToLower().Contains(n) || p.Content.ToLower().Contains(n)))
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .Take(80)
            .ToListAsync(cancellationToken);

        var dtos = await PostEnricher.ToPostDtosAsync(_db, posts, viewerId, cancellationToken);
        return Ok(new { posts = dtos });
    }
}
