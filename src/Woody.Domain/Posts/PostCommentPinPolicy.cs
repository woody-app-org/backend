using Woody.Domain.Entities;

namespace Woody.Domain.Posts;

/// <summary>
/// Regras puras para destacar um comentário no post: só a autora do post; comentário raiz visível.
/// </summary>
public static class PostCommentPinPolicy
{
    public const int MaxPinnedCommentsPerPost = 1;

    public static bool CanPinCommentOnPost(int actingUserId, Post post, Comment comment) =>
        post.DeletedAt == null
        && comment.DeletedAt == null
        && comment.PostId == post.Id
        && post.UserId == actingUserId
        && comment.ParentCommentId == null
        && comment.HiddenByPostAuthorAt == null;

    /// <summary>Desafixar pode ser feito pela autora do post para qualquer comentário do post (idempotente se não estava fixo).</summary>
    public static bool CanUnpinCommentOnPost(int actingUserId, Post post, Comment comment) =>
        post.DeletedAt == null
        && comment.DeletedAt == null
        && comment.PostId == post.Id
        && post.UserId == actingUserId;
}
