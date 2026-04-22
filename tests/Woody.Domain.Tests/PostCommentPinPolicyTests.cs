using Woody.Domain.Entities;
using Woody.Domain.Posts;

namespace Woody.Domain.Tests;

public sealed class PostCommentPinPolicyTests
{
    [Fact]
    public void CanPin_root_visible_comment_by_post_author()
    {
        var post = new Post { Id = 1, UserId = 10, DeletedAt = null };
        var comment = new Comment
        {
            Id = 3,
            PostId = 1,
            AuthorId = 99,
            ParentCommentId = null,
            HiddenByPostAuthorAt = null,
            DeletedAt = null
        };
        Assert.True(PostCommentPinPolicy.CanPinCommentOnPost(10, post, comment));
    }

    [Fact]
    public void CannotPin_if_not_post_author()
    {
        var post = new Post { Id = 1, UserId = 10, DeletedAt = null };
        var comment = new Comment { PostId = 1, ParentCommentId = null, HiddenByPostAuthorAt = null, DeletedAt = null };
        Assert.False(PostCommentPinPolicy.CanPinCommentOnPost(11, post, comment));
    }

    [Fact]
    public void CannotPin_reply()
    {
        var post = new Post { Id = 1, UserId = 10, DeletedAt = null };
        var comment = new Comment
        {
            PostId = 1,
            ParentCommentId = 2,
            HiddenByPostAuthorAt = null,
            DeletedAt = null
        };
        Assert.False(PostCommentPinPolicy.CanPinCommentOnPost(10, post, comment));
    }

    [Fact]
    public void CannotPin_hidden_comment()
    {
        var post = new Post { Id = 1, UserId = 10, DeletedAt = null };
        var comment = new Comment
        {
            PostId = 1,
            ParentCommentId = null,
            HiddenByPostAuthorAt = DateTime.UtcNow,
            DeletedAt = null
        };
        Assert.False(PostCommentPinPolicy.CanPinCommentOnPost(10, post, comment));
    }

    [Fact]
    public void CanUnpin_post_author_even_if_reply()
    {
        var post = new Post { Id = 1, UserId = 10, DeletedAt = null };
        var comment = new Comment { PostId = 1, ParentCommentId = 2, DeletedAt = null };
        Assert.True(PostCommentPinPolicy.CanUnpinCommentOnPost(10, post, comment));
    }
}
