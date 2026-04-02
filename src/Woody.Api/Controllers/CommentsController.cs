using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public CommentsController(WoodyDbContext db)
    {
        _db = db;
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

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == cid, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return NotFound();

        if (comment.AuthorId != me.Value)
            return Forbid();

        comment.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
