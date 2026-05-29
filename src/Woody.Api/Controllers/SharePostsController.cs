using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

/// <summary>
/// Páginas HTML públicas para preview Open Graph de publicações partilhadas externamente.
/// </summary>
[AllowAnonymous]
[Route("share/posts")]
public sealed class SharePostsController : Controller
{
    private readonly IPostSharePageService _sharePages;

    public SharePostsController(IPostSharePageService sharePages)
    {
        _sharePages = sharePages;
    }

    [HttpGet("{publicId}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    [Produces("text/html")]
    public async Task<IActionResult> GetSharePage(string publicId, CancellationToken cancellationToken)
    {
        var origin = BuildRequestOrigin(Request);
        var model = await _sharePages.BuildPageModelAsync(publicId, origin, cancellationToken);
        var html = _sharePages.RenderHtml(model);
        return Content(html, "text/html; charset=utf-8");
    }

    internal static string BuildRequestOrigin(HttpRequest request)
    {
        var scheme = request.Scheme;
        var host = request.Host.Value;
        if (!string.IsNullOrWhiteSpace(host))
            return $"{scheme}://{host}".TrimEnd('/');
        return "http://localhost:5000";
    }
}
