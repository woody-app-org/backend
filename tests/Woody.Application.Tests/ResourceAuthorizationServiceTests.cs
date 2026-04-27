using Moq;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class ResourceAuthorizationServiceTests
{
    [Fact]
    public async Task CanEditPostAsync_AllowsAuthor()
    {
        var sut = CreateService();
        var post = new Post { UserId = 10 };

        var allowed = await sut.CanEditPostAsync(post, actorUserId: 10);

        Assert.True(allowed);
    }

    [Fact]
    public async Task CanEditPostAsync_BlocksNonOwner()
    {
        var sut = CreateService();
        var post = new Post { UserId = 10 };

        var allowed = await sut.CanEditPostAsync(post, actorUserId: 20);

        Assert.False(allowed);
    }

    [Fact]
    public async Task CanEditPostAsync_AllowsGlobalAdmin()
    {
        var sut = CreateService(adminUserId: 20);
        var post = new Post { UserId = 10 };

        var allowed = await sut.CanEditPostAsync(post, actorUserId: 20);

        Assert.True(allowed);
    }

    [Fact]
    public async Task CanDeleteCommentAsync_AllowsCommunityModerator()
    {
        var sut = CreateService(moderatorUserId: 20, moderatedCommunityId: 5);
        var comment = new Comment
        {
            AuthorId = 10,
            Post = new Post
            {
                UserId = 10,
                CommunityId = 5,
                PublicationContext = PostPublicationContext.Community,
                Community = new Community { Id = 5, Visibility = "private" }
            }
        };

        var allowed = await sut.CanDeleteCommentAsync(comment, actorUserId: 20);

        Assert.True(allowed);
    }

    [Fact]
    public async Task CanReadPostAsync_BlocksNonMemberFromPrivateCommunity()
    {
        var sut = CreateService();
        var post = new Post
        {
            UserId = 10,
            CommunityId = 5,
            PublicationContext = PostPublicationContext.Community,
            Community = new Community { Id = 5, Visibility = "private" }
        };

        var allowed = await sut.CanReadPostAsync(post, viewerUserId: 20);

        Assert.False(allowed);
    }

    private static ResourceAuthorizationService CreateService(
        int? adminUserId = null,
        int? moderatorUserId = null,
        int? moderatedCommunityId = null)
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdNoTrackingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new User
            {
                Id = id,
                Role = adminUserId == id ? "Admin" : "User",
                Username = $"user{id}",
                Email = $"user{id}@example.com"
            });

        var memberships = new Mock<ICommunityMembershipRepository>();
        memberships
            .Setup(x => x.GetActiveForUserAndCommunityNoTrackingAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, int communityId, CancellationToken _) =>
                moderatorUserId == userId && moderatedCommunityId == communityId
                    ? new CommunityMembership { UserId = userId, CommunityId = communityId, Role = "admin", Status = "active" }
                    : null);

        return new ResourceAuthorizationService(users.Object, memberships.Object);
    }
}
