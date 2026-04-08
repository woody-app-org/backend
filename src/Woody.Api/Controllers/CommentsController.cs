using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _comments;

    public CommentsController(ICommentRepository comments)
    {
        _comments = comments;
    }

    [Authorize]
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> Delete(string commentId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(commentId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var comment = await _comments.GetTrackedAsync(cid, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return NotFound();

        if (comment.AuthorId != me.Value)
            return Forbid();

        comment.DeletedAt = DateTime.UtcNow;
        await _comments.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
