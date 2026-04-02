using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public ReportsController(WoodyDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReportRequestDTO body, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        int? postId = null;
        int? commentId = null;
        if (!string.IsNullOrWhiteSpace(body.PostId) && int.TryParse(body.PostId, out var pid))
            postId = pid;
        if (!string.IsNullOrWhiteSpace(body.CommentId) && int.TryParse(body.CommentId, out var cid))
            commentId = cid;

        var target = body.TargetType.Trim().ToLowerInvariant();
        if (target == "post" && postId == null)
            return BadRequest();
        if (target == "comment" && (commentId == null || postId == null))
            return BadRequest();

        _db.ContentReports.Add(new ContentReport
        {
            ReporterUserId = me.Value,
            TargetType = target,
            PostId = postId,
            CommentId = commentId,
            ReasonCode = body.ReasonCode.Trim(),
            Details = string.IsNullOrWhiteSpace(body.Details) ? null : body.Details.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
