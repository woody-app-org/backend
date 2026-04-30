using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Posts;
using Woody.Domain.Entities.Enum;
using Woody.Application.Mapping;
using Woody.Application.Validation;

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
    private readonly IContentPinningService _pinning;
    private readonly IResourceAuthorizationService _authorization;
    private readonly IUserNotificationService _userNotifications;

    public PostsController(
        IPostRepository posts,
        ICommunityRepository communities,
        ICommunityPermissionService communityPermissions,
        ILikeRepository likes,
        ICommentRepository comments,
        IPostEnrichmentService postEnrichment,
        IContentPinningService pinning,
        IResourceAuthorizationService authorization,
        IUserNotificationService userNotifications)
    {
        _posts = posts;
        _communities = communities;
        _communityPermissions = communityPermissions;
        _likes = likes;
        _comments = comments;
        _postEnrichment = postEnrichment;
        _pinning = pinning;
        _authorization = authorization;
        _userNotifications = userNotifications;
    }

    private IActionResult FromPinningOutcome(ContentPinningOutcome outcome) => outcome switch
    {
        ContentPinningOutcome.Success => NoContent(),
        ContentPinningOutcome.PostNotFound => NotFound(),
        ContentPinningOutcome.CommentNotFound => NotFound(),
        ContentPinningOutcome.Forbidden => Forbid(),
        ContentPinningOutcome.ProfilePinLimitReached => Conflict(new
        {
            error = $"Só podes fixar até {PostProfilePinPolicy.MaxPinnedPostsOnProfile} publicações no teu perfil."
        }),
        ContentPinningOutcome.CommentNotEligible => BadRequest(new
        {
            error = "Só é possível fixar um comentário raiz que esteja visível (não oculto)."
        }),
        _ => BadRequest()
    };

    [AllowAnonymous]
    [HttpGet("{postId}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<PostResponseDto>> GetById(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

        var post = await _posts.GetByIdNonDeletedWithNavAsync(pid, cancellationToken);
        if (post == null)
            return NotFound();
        if (!await _authorization.CanReadPostAsync(post, viewerId, cancellationToken))
            return NotFound();

        var list = await _postEnrichment.ToPostDtosAsync(new[] { post }, viewerId, cancellationToken);
        return Ok(list[0]);
    }

    [Authorize]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.ContentCreate)]
    public async Task<ActionResult<PostResponseDto>> Create([FromBody] CreatePostRequestDTO body, CancellationToken cancellationToken)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        if (!InputValidator.TryNormalizeRequiredText(
                body.Title,
                "Título",
                InputValidationLimits.PostTitleMaxLength,
                out var title,
                out var error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeRequiredText(
                body.Content,
                "Conteúdo",
                InputValidationLimits.PostContentMaxLength,
                out var content,
                out error))
            return BadRequest(new { error });

        if (!TryNormalizePostImageUrls(body, out var imageUrls, out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeTags(
                body.Tags,
                InputValidationLimits.PostTagsMaxCount,
                InputValidationLimits.TagMaxLength,
                out var tags,
                out error))
            return BadRequest(new { error });

        var ctxRaw = (body.PublicationContext ?? string.Empty).Trim().ToLowerInvariant();
        var hasCommunityId = !string.IsNullOrWhiteSpace(body.CommunityId);

        bool useProfile;
        if (string.IsNullOrEmpty(ctxRaw))
        {
            useProfile = !hasCommunityId;
        }
        else if (ctxRaw == "profile")
        {
            if (hasCommunityId)
                return BadRequest(new { error = "Publicações de perfil não devem incluir comunidade." });
            useProfile = true;
        }
        else if (ctxRaw == "community")
        {
            if (!hasCommunityId)
                return BadRequest(new { error = "Escolha uma comunidade para publicar." });
            useProfile = false;
        }
        else
            return BadRequest(new { error = "Contexto de publicação inválido. Use \"profile\" ou \"community\"." });

        Post post;
        if (useProfile)
        {
            post = new Post
            {
                UserId = me.Value,
                CommunityId = null,
                PublicationContext = PostPublicationContext.Profile,
                Title = title,
                Content = content,
                ImageUrl = imageUrls.Count > 0 ? imageUrls[0] : null,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            if (!int.TryParse(body.CommunityId!.Trim(), out var communityId) || communityId <= 0)
                return BadRequest(new { error = "Identificador de comunidade inválido." });

            if (!await _communities.ExistsNoTrackingAsync(communityId, cancellationToken))
                return NotFound();

            if (!await _communityPermissions.CanPublishPostAsync(communityId, me.Value, cancellationToken))
                return Forbid();

            post = new Post
            {
                UserId = me.Value,
                CommunityId = communityId,
                PublicationContext = PostPublicationContext.Community,
                Title = title,
                Content = content,
                ImageUrl = imageUrls.Count > 0 ? imageUrls[0] : null,
                CreatedAt = DateTime.UtcNow
            };
        }
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

        if (tags.Count > 0)
        {
            var rows = tags.Select(t => new PostTag { PostId = post.Id, Tag = t });
            await _posts.AddPostTagsAsync(rows, cancellationToken);
            await _posts.SaveChangesAsync(cancellationToken);
        }

        post = await _posts.GetByIdNonDeletedWithNavAsync(post.Id, cancellationToken)
               ?? throw new InvalidOperationException("Post not found after create.");

        var dto = await _postEnrichment.ToPostDtosAsync(new[] { post }, me, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { postId = post.Id.ToString() }, dto[0]);
    }

    private static bool TryNormalizePostImageUrls(
        CreatePostRequestDTO body,
        out List<string> normalized,
        out string? error)
    {
        normalized = new List<string>();
        error = null;

        if (body.ImageUrls is { Count: > 0 })
        {
            foreach (var u in body.ImageUrls)
            {
                if (!InputValidator.TryNormalizeHttpsImageUrl(u, out var imageUrl, out error))
                    return false;
                if (imageUrl == null || normalized.Contains(imageUrl))
                    continue;
                normalized.Add(imageUrl);
                if (normalized.Count > MaxPostImages)
                {
                    error = $"Máximo de {MaxPostImages} imagens por publicação.";
                    return false;
                }
            }

            return true;
        }

        if (!InputValidator.TryNormalizeHttpsImageUrl(body.ImageUrl, out var singleImageUrl, out error))
            return false;
        if (singleImageUrl != null)
            normalized.Add(singleImageUrl);
        return true;
    }

    [Authorize]
    [HttpPatch("{postId}")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
        if (!await _authorization.CanEditPostAsync(post, me.Value, cancellationToken))
            return Forbid();

        if (!InputValidator.TryNormalizeRequiredText(
                body.Title,
                "Título",
                InputValidationLimits.PostTitleMaxLength,
                out var title,
                out var error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeRequiredText(
                body.Content,
                "Conteúdo",
                InputValidationLimits.PostContentMaxLength,
                out var content,
                out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeHttpsImageUrl(body.ImageUrl, out var imageUrl, out error))
            return BadRequest(new { error });

        if (!InputValidator.TryNormalizeTags(
                body.Tags,
                InputValidationLimits.PostTagsMaxCount,
                InputValidationLimits.TagMaxLength,
                out var tags,
                out error))
            return BadRequest(new { error });

        post.Title = title;
        post.Content = content;
        post.ImageUrl = imageUrl;
        post.UpdatedAt = DateTime.UtcNow;

        if (body.Tags != null)
        {
            _posts.RemovePostTags(post.Tags);
            var newTags = tags.Select(t => new PostTag { PostId = post.Id, Tag = t });
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
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
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
        if (!await _authorization.CanDeletePostAsync(post, me.Value, cancellationToken))
            return Forbid();

        post.DeletedAt = DateTime.UtcNow;
        await _posts.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{postId}/like")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> Like(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var postForLike = await _posts.GetByIdNonDeletedForCommentLookupAsync(pid, cancellationToken);
        if (postForLike == null || !await _authorization.CanReadPostAsync(postForLike, me, cancellationToken))
            return NotFound();

        var added = await _likes.TryAddPostLikeAsync(me.Value, pid, cancellationToken);
        if (added)
            await _userNotifications.NotifyPostLikedAsync(me.Value, postForLike.UserId, pid, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{postId}/like")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<IActionResult> Unlike(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var postForUnlike = await _posts.GetByIdNonDeletedForCommentLookupAsync(pid, cancellationToken);
        if (!await _authorization.CanReadPostAsync(postForUnlike, me, cancellationToken))
            return NotFound();

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
    [EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
    public async Task<ActionResult<List<CommentResponseDto>>> GetComments(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var post = await _posts.GetByIdNonDeletedForCommentLookupAsync(pid, cancellationToken);
        var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        if (!await _authorization.CanReadPostAsync(post, viewerId, cancellationToken))
            return NotFound();

        var postAuthorId = post!.UserId;
        var comments = await _comments.ListActiveForPostWithAuthorAsync(pid, cancellationToken);

        return Ok(comments.Select(c => EntityMappers.ToCommentDto(c, postAuthorId, viewerId)).ToList());
    }

    [Authorize]
    [HttpPost("{postId}/comments")]
    [EnableRateLimiting(RateLimitPolicyNames.ContentComment)]
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
        if (!await _authorization.CanReadPostAsync(post, me, cancellationToken))
            return NotFound();

        var postAuthorId = post!.UserId;

        if (!InputValidator.TryNormalizeRequiredText(
                body.Content,
                "Comentário",
                InputValidationLimits.CommentContentMaxLength,
                out var commentContent,
                out var error))
            return BadRequest(new { error });

        int? parentId = null;
        Comment? parentComment = null;
        if (!string.IsNullOrWhiteSpace(body.ParentCommentId))
        {
            if (!int.TryParse(body.ParentCommentId, out var pcid) || pcid <= 0)
                return BadRequest(new { error = "Comentário pai inválido." });

            parentComment = await _comments.GetByIdNonDeletedWithAuthorAsync(pcid, cancellationToken);
            if (parentComment == null || parentComment.PostId != pid)
                return BadRequest(new { error = "Comentário pai inválido." });
            parentId = pcid;
        }

        var comment = new Comment
        {
            PostId = pid,
            AuthorId = me.Value,
            ParentCommentId = parentId,
            Content = commentContent,
            CreatedAt = DateTime.UtcNow
        };
        _comments.Add(comment);
        await _comments.SaveChangesAsync(cancellationToken);

        if (parentId == null)
        {
            await _userNotifications.NotifyPostCommentAsync(me.Value, postAuthorId, pid, comment.Id, cancellationToken);
        }
        else if (parentComment != null)
        {
            await _userNotifications.NotifyCommentReplyAsync(
                me.Value,
                parentComment.AuthorId,
                pid,
                parentComment.Id,
                comment.Id,
                cancellationToken);
        }

        comment = await _comments.GetByIdNonDeletedWithAuthorAsync(comment.Id, cancellationToken)
                  ?? throw new InvalidOperationException("Comment not found after create.");

        return Ok(EntityMappers.ToCommentDto(comment, postAuthorId, me));
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
        if (!await _authorization.CanModeratePostCommentsAsync(post, me.Value, cancellationToken))
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

    [Authorize]
    [HttpPost("{postId}/profile-pin")]
    public async Task<IActionResult> PinOnProfile(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var outcome = await _pinning.PinPostOnProfileAsync(me.Value, pid, cancellationToken);
        return FromPinningOutcome(outcome);
    }

    [Authorize]
    [HttpDelete("{postId}/profile-pin")]
    public async Task<IActionResult> UnpinFromProfile(string postId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var outcome = await _pinning.UnpinPostOnProfileAsync(me.Value, pid, cancellationToken);
        return FromPinningOutcome(outcome);
    }

    [Authorize]
    [HttpPost("{postId}/comments/{commentId}/pin")]
    public async Task<IActionResult> PinComment(string postId, string commentId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid) || !int.TryParse(commentId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var outcome = await _pinning.PinCommentOnPostAsync(me.Value, pid, cid, cancellationToken);
        return FromPinningOutcome(outcome);
    }

    [Authorize]
    [HttpDelete("{postId}/comments/{commentId}/pin")]
    public async Task<IActionResult> UnpinComment(string postId, string commentId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(postId, out var pid) || !int.TryParse(commentId, out var cid))
            return BadRequest();

        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var outcome = await _pinning.UnpinCommentOnPostAsync(me.Value, pid, cid, cancellationToken);
        return FromPinningOutcome(outcome);
    }
}
