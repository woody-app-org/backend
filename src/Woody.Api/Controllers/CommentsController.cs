using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize(Policy = "VerifiedAccount")]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _comments;
    private readonly IResourceAuthorizationService _authorization;

    public CommentsController(
        ICommentRepository comments,
        IResourceAuthorizationService authorization)
    {
        _comments = comments;
        _authorization = authorization;
    }

    [Authorize]
    [HttpDelete("{commentId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> Delete(string commentId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(commentId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var comment = await _comments.GetTrackedWithPostAsync(cid, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return NotFound();

        if (!await _authorization.CanDeleteCommentAsync(comment, me.Value, cancellationToken))
            return Forbid();

        comment.DeletedAt = DateTime.UtcNow;
        await _comments.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
