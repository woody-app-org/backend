using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Posts;

namespace Woody.Domain.Tests;

public sealed class PostProfilePinPolicyTests
{
    [Fact]
    public void CanPin_own_non_deleted_post()
    {
        var post = new Post { UserId = 5, DeletedAt = null };
        Assert.True(PostProfilePinPolicy.CanPinPostOnProfile(5, post));
    }

    [Fact]
    public void CannotPin_others_post()
    {
        var post = new Post { UserId = 5, DeletedAt = null };
        Assert.False(PostProfilePinPolicy.CanPinPostOnProfile(9, post));
    }

    [Fact]
    public void CannotPin_deleted_post()
    {
        var post = new Post { UserId = 5, DeletedAt = DateTime.UtcNow };
        Assert.False(PostProfilePinPolicy.CanPinPostOnProfile(5, post));
    }

    [Fact]
    public void CanPin_community_post_authored_by_self()
    {
        var post = new Post
        {
            UserId = 2,
            DeletedAt = null,
            PublicationContext = PostPublicationContext.Community,
            CommunityId = 1
        };
        Assert.True(PostProfilePinPolicy.CanPinPostOnProfile(2, post));
    }
}
