using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IContentReportRepository _reports;
    private readonly IPostRepository _posts;
    private readonly ICommentRepository _comments;
    private readonly IResourceAuthorizationService _authorization;

    public ReportsController(
        IContentReportRepository reports,
        IPostRepository posts,
        ICommentRepository comments,
        IResourceAuthorizationService authorization)
    {
        _reports = reports;
        _posts = posts;
        _comments = comments;
        _authorization = authorization;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReportRequestDTO body, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(body.TargetType) || string.IsNullOrWhiteSpace(body.ReasonCode))
            return BadRequest();

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

        if (target == "post")
        {
            var post = await _posts.GetByIdNonDeletedForCommentLookupAsync(postId!.Value, cancellationToken);
            if (!await _authorization.CanReadPostAsync(post, me.Value, cancellationToken))
                return NotFound();
        }
        else if (target == "comment")
        {
            var comment = await _comments.GetTrackedWithPostAsync(commentId!.Value, cancellationToken);
            if (comment == null || comment.DeletedAt != null || comment.PostId != postId!.Value)
                return NotFound();
            if (!await _authorization.CanReadPostAsync(comment.Post, me.Value, cancellationToken))
                return NotFound();
        }
        else
        {
            return BadRequest();
        }

        _reports.Add(new ContentReport
        {
            ReporterUserId = me.Value,
            TargetType = target,
            PostId = postId,
            CommentId = commentId,
            ReasonCode = body.ReasonCode.Trim(),
            Details = string.IsNullOrWhiteSpace(body.Details) ? null : body.Details.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _reports.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
