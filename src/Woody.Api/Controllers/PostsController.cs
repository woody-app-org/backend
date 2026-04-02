using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Mapping;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly WoodyDbContext _db;

    public PostsController(WoodyDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("{postId}")]
    public async Task<ActionResult<PostResponseDto>> GetById(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var post = await _db.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == pid && p.DeletedAt == null, cancellationToken);
        if (post == null)
            return NotFound();

        var list = await PostEnricher.ToPostDtosAsync(_db, new[] { post }, viewerId, cancellationToken);
        return Ok(list[0]);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PostResponseDto>> Create([FromBody] CreatePostRequestDTO body, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!int.TryParse(body.CommunityId, out var communityId))
            return BadRequest();

        var member = await _db.CommunityMemberships.FirstOrDefaultAsync(
            m => m.CommunityId == communityId && m.UserId == me.Value && m.Status == "active",
            cancellationToken);
        if (member == null)
            return Forbid();

        var post = new Post
        {
            UserId = me.Value,
            CommunityId = communityId,
            Title = body.Title.Trim(),
            Content = body.Content.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(body.ImageUrl) ? null : body.ImageUrl.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);

        if (body.Tags != null)
        {
            foreach (var t in body.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                _db.PostTags.Add(new PostTag { PostId = post.Id, Tag = t.Trim() });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        post = await _db.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .FirstAsync(p => p.Id == post.Id, cancellationToken);

        var dto = await PostEnricher.ToPostDtosAsync(_db, new[] { post }, me, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { postId = post.Id.ToString() }, dto[0]);
    }

    [Authorize]
    [HttpPatch("{postId}")]
    public async Task<ActionResult<PostResponseDto>> Update(
        string postId,
        [FromBody] UpdatePostRequestDTO body,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var post = await _db.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return NotFound();
        if (post.UserId != me.Value)
            return Forbid();

        post.Title = body.Title.Trim();
        post.Content = body.Content.Trim();
        post.ImageUrl = string.IsNullOrWhiteSpace(body.ImageUrl) ? null : body.ImageUrl.Trim();
        post.UpdatedAt = DateTime.UtcNow;

        if (body.Tags != null)
        {
            _db.PostTags.RemoveRange(post.Tags);
            foreach (var t in body.Tags.Where(x => !string.IsNullOrWhiteSpace(x)))
                _db.PostTags.Add(new PostTag { PostId = post.Id, Tag = t.Trim() });
        }

        await _db.SaveChangesAsync(cancellationToken);

        post = await _db.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .FirstAsync(p => p.Id == pid, cancellationToken);

        var dto = await PostEnricher.ToPostDtosAsync(_db, new[] { post }, me, cancellationToken);
        return Ok(dto[0]);
    }

    [Authorize]
    [HttpDelete("{postId}")]
    public async Task<IActionResult> Delete(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return NotFound();
        if (post.UserId != me.Value)
            return Forbid();

        post.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{postId}/like")]
    public async Task<IActionResult> Like(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var exists = await _db.Likes.AnyAsync(
            l => l.UserId == me.Value && l.TargetType == LikeTargetType.Post && l.TargetId == pid,
            cancellationToken);
        if (exists)
            return NoContent();

        _db.Likes.Add(new Like
        {
            UserId = me.Value,
            TargetType = LikeTargetType.Post,
            TargetId = pid,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{postId}/like")]
    public async Task<IActionResult> Unlike(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var row = await _db.Likes.FirstOrDefaultAsync(
            l => l.UserId == me.Value && l.TargetType == LikeTargetType.Post && l.TargetId == pid,
            cancellationToken);
        if (row != null)
        {
            _db.Likes.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{postId}/comments")]
    public async Task<ActionResult<List<CommentResponseDto>>> GetComments(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return NotFound();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var comments = await _db.Comments.AsNoTracking()
            .Where(c => c.PostId == pid && c.DeletedAt == null)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(comments.Select(c => EntityMappers.ToCommentDto(c, post.UserId, viewerId)).ToList());
    }

    [Authorize]
    [HttpPost("{postId}/comments")]
    public async Task<ActionResult<CommentResponseDto>> CreateComment(
        string postId,
        [FromBody] CreateCommentRequestDTO body,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid && p.DeletedAt == null, cancellationToken);
        if (post == null)
            return NotFound();

        int? parentId = null;
        if (!string.IsNullOrWhiteSpace(body.ParentCommentId) && int.TryParse(body.ParentCommentId, out var pcid))
            parentId = pcid;

        var comment = new Comment
        {
            PostId = pid,
            AuthorId = me.Value,
            ParentCommentId = parentId,
            Content = body.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        comment = await _db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id, cancellationToken);

        return Ok(EntityMappers.ToCommentDto(comment, post.UserId, me));
    }

    [Authorize]
    [HttpPost("{postId}/comments/{commentId}/hide")]
    public async Task<ActionResult<CommentResponseDto>> HideComment(
        string postId,
        string commentId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid) || !int.TryParse(commentId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == pid && p.DeletedAt == null, cancellationToken);
        if (post == null)
            return NotFound();
        if (post.UserId != me.Value)
            return Forbid();

        var comment = await _db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == cid && c.PostId == pid, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return NotFound();

        comment.HiddenByPostAuthorAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        comment = await _db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == cid, cancellationToken);

        return Ok(EntityMappers.ToCommentDto(comment, post.UserId, me));
    }
}
