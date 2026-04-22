using Woody.Application.Interfaces;
using Woody.Domain.Posts;

namespace Woody.Application.Services;

public class ContentPinningService : IContentPinningService
{
    private readonly IPostRepository _posts;
    private readonly ICommentRepository _comments;

    public ContentPinningService(IPostRepository posts, ICommentRepository comments)
    {
        _posts = posts;
        _comments = comments;
    }

    public async Task<ContentPinningOutcome> PinPostOnProfileAsync(int actingUserId, int postId, CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetByIdTrackedAsync(postId, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return ContentPinningOutcome.PostNotFound;
        if (!PostProfilePinPolicy.CanPinPostOnProfile(actingUserId, post))
            return ContentPinningOutcome.Forbidden;
        if (post.PinnedOnProfileAt != null)
            return ContentPinningOutcome.Success;

        var count = await _posts.CountPinnedPostsForAuthorAsync(actingUserId, cancellationToken);
        if (count >= PostProfilePinPolicy.MaxPinnedPostsOnProfile)
            return ContentPinningOutcome.ProfilePinLimitReached;

        post.PinnedOnProfileAt = DateTime.UtcNow;
        await _posts.SaveChangesAsync(cancellationToken);
        return ContentPinningOutcome.Success;
    }

    public async Task<ContentPinningOutcome> UnpinPostOnProfileAsync(int actingUserId, int postId, CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetByIdTrackedAsync(postId, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return ContentPinningOutcome.PostNotFound;
        if (!PostProfilePinPolicy.CanUnpinPostOnProfile(actingUserId, post))
            return ContentPinningOutcome.Forbidden;

        post.PinnedOnProfileAt = null;
        await _posts.SaveChangesAsync(cancellationToken);
        return ContentPinningOutcome.Success;
    }

    public async Task<ContentPinningOutcome> PinCommentOnPostAsync(int actingUserId, int postId, int commentId, CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetByIdTrackedAsync(postId, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return ContentPinningOutcome.PostNotFound;

        var comment = await _comments.GetTrackedWithAuthorAsync(commentId, postId, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return ContentPinningOutcome.CommentNotFound;

        if (post.UserId != actingUserId)
            return ContentPinningOutcome.Forbidden;
        if (!PostCommentPinPolicy.CanPinCommentOnPost(actingUserId, post, comment))
            return ContentPinningOutcome.CommentNotEligible;

        if (comment.PinnedOnPostAt != null)
            return ContentPinningOutcome.Success;

        var previous = await _comments.GetTrackedPinnedCommentForPostAsync(postId, cancellationToken);
        if (previous != null && previous.Id != commentId)
            previous.PinnedOnPostAt = null;

        comment.PinnedOnPostAt = DateTime.UtcNow;
        await _comments.SaveChangesAsync(cancellationToken);
        return ContentPinningOutcome.Success;
    }

    public async Task<ContentPinningOutcome> UnpinCommentOnPostAsync(int actingUserId, int postId, int commentId, CancellationToken cancellationToken = default)
    {
        var post = await _posts.GetByIdTrackedAsync(postId, cancellationToken);
        if (post == null || post.DeletedAt != null)
            return ContentPinningOutcome.PostNotFound;

        var comment = await _comments.GetTrackedWithAuthorAsync(commentId, postId, cancellationToken);
        if (comment == null || comment.DeletedAt != null)
            return ContentPinningOutcome.CommentNotFound;

        if (!PostCommentPinPolicy.CanUnpinCommentOnPost(actingUserId, post, comment))
            return ContentPinningOutcome.Forbidden;

        comment.PinnedOnPostAt = null;
        await _comments.SaveChangesAsync(cancellationToken);
        return ContentPinningOutcome.Success;
    }
}
