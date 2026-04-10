using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Application.Mapping;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private const int MaxPostImages = 20;

    private readonly IPostRepository _posts;
    private readonly ICommunityRepository _communities;
    private readonly ICommunityPermissionService _communityPermissions;
    private readonly ILikeRepository _likes;
    private readonly ICommentRepository _comments;
    private readonly IPostEnrichmentService _postEnrichment;

    public PostsController(
        IPostRepository posts,
        ICommunityRepository communities,
        ICommunityPermissionService communityPermissions,
        ILikeRepository likes,
        ICommentRepository comments,
        IPostEnrichmentService postEnrichment)
    {
        _posts = posts;
        _communities = communities;
        _communityPermissions = communityPermissions;
        _likes = likes;
        _comments = comments;
        _postEnrichment = postEnrichment;
    }

    [AllowAnonymous]
    [HttpGet("{postId}")]
    public async Task<ActionResult<PostResponseDto>> GetById(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var post = await _posts.GetByIdNonDeletedWithNavAsync(pid, cancellationToken);
        if (post == null)
            return NotFound();

        var list = await _postEnrichment.ToPostDtosAsync(new[] { post }, viewerId, cancellationToken);
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
            return BadRequest(new { error = "Identificador de comunidade inválido." });

        if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Content))
            return BadRequest(new { error = "Título e conteúdo são obrigatórios." });

        if (!await _communities.ExistsNoTrackingAsync(communityId, cancellationToken))
            return NotFound();

        if (!await _communityPermissions.CanPublishPostAsync(communityId, me.Value, cancellationToken))
            return Forbid();

        var imageUrls = NormalizePostImageUrls(body);
        if (imageUrls.Count > MaxPostImages)
            return BadRequest(new { error = $"Máximo de {MaxPostImages} imagens por publicação." });

        var post = new Post
        {
            UserId = me.Value,
            CommunityId = communityId,
            Title = body.Title.Trim(),
            Content = body.Content.Trim(),
            ImageUrl = imageUrls.Count > 0 ? imageUrls[0] : null,
            CreatedAt = DateTime.UtcNow
        };
        _posts.Add(post);
        await _posts.SaveChangesAsync(cancellationToken);

        if (imageUrls.Count > 0)
        {
            var rows = imageUrls.Select((url, i) => new PostImage
            {
                PostId = post.Id,
                Url = url,
                DisplayOrder = i
            });
            await _posts.AddPostImagesAsync(rows, cancellationToken);
            await _posts.SaveChangesAsync(cancellationToken);
        }

        if (body.Tags != null)
        {
            var tags = body.Tags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(t => new PostTag { PostId = post.Id, Tag = t.Trim() });
            await _posts.AddPostTagsAsync(tags, cancellationToken);
            await _posts.SaveChangesAsync(cancellationToken);
        }

        post = await _posts.GetByIdNonDeletedWithNavAsync(post.Id, cancellationToken)
               ?? throw new InvalidOperationException("Post not found after create.");

        var dto = await _postEnrichment.ToPostDtosAsync(new[] { post }, me, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { postId = post.Id.ToString() }, dto[0]);
    }

    private static List<string> NormalizePostImageUrls(CreatePostRequestDTO body)
    {
        var list = new List<string>();
        if (body.ImageUrls is { Count: > 0 })
        {
            foreach (var u in body.ImageUrls)
            {
                if (string.IsNullOrWhiteSpace(u))
                    continue;
                list.Add(u.Trim());
            }

            return list;
        }

        if (!string.IsNullOrWhiteSpace(body.ImageUrl))
            list.Add(body.ImageUrl.Trim());
        return list;
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

        var post = await _posts.GetByIdTrackedWithTagsAsync(pid, cancellationToken);
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
            _posts.RemovePostTags(post.Tags);
            var newTags = body.Tags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(t => new PostTag { PostId = post.Id, Tag = t.Trim() });
            await _posts.AddPostTagsAsync(newTags, cancellationToken);
        }

        await _posts.SaveChangesAsync(cancellationToken);

        post = await _posts.GetByIdNonDeletedWithNavAsync(pid, cancellationToken)
               ?? throw new InvalidOperationException("Post not found after update.");

        var dto = await _postEnrichment.ToPostDtosAsync(new[] { post }, me, cancellationToken);
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

        var post = await _posts.GetByIdTrackedAsync(pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return NotFound();
        if (post.UserId != me.Value)
            return Forbid();

        post.DeletedAt = DateTime.UtcNow;
        await _posts.SaveChangesAsync(cancellationToken);
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

        if (await _likes.ExistsPostLikeAsync(me.Value, pid, cancellationToken))
            return NoContent();

        _likes.Add(new Like
        {
            UserId = me.Value,
            TargetType = LikeTargetType.Post,
            TargetId = pid,
            CreatedAt = DateTime.UtcNow
        });
        await _likes.SaveChangesAsync(cancellationToken);
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

        var row = await _likes.GetPostLikeAsync(me.Value, pid, cancellationToken);
        if (row != null)
        {
            _likes.Remove(row);
            await _likes.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{postId}/comments")]
    public async Task<ActionResult<List<CommentResponseDto>>> GetComments(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var post = await _posts.GetByIdNonDeletedForCommentLookupAsync(pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return NotFound();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var comments = await _comments.ListActiveForPostWithAuthorAsync(pid, cancellationToken);

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

        var post = await _posts.GetByIdNonDeletedForCommentLookupAsync(pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
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
        _comments.Add(comment);
        await _comments.SaveChangesAsync(cancellationToken);

        comment = await _comments.GetByIdNonDeletedWithAuthorAsync(comment.Id, cancellationToken)
                  ?? throw new InvalidOperationException("Comment not found after create.");

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

        var post = await _posts.GetByIdTrackedAsync(pid, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return NotFound();
        if (post.UserId != me.Value)
            return Forbid();

        var comment = await _comments.GetTrackedWithAuthorAsync(cid, pid, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return NotFound();

        comment.HiddenByPostAuthorAt = DateTime.UtcNow;
        await _comments.SaveChangesAsync(cancellationToken);

        comment = await _comments.GetByIdNonDeletedWithAuthorAsync(cid, cancellationToken)
                  ?? throw new InvalidOperationException("Comment not found after hide.");

        return Ok(EntityMappers.ToCommentDto(comment, post.UserId, me));
    }
}
