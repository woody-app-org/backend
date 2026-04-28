using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly IUserRepository _users;
    private readonly ICommunityMembershipRepository _memberships;

    public ResourceAuthorizationService(
        IUserRepository users,
        ICommunityMembershipRepository memberships)
    {
        _users = users;
        _memberships = memberships;
    }

    public async Task<bool> CanReadPostAsync(Post? post, int? viewerUserId, CancellationToken cancellationToken = default)
    {
        if (post == null || post.DeletedAt != null)
            return false;
        if (post.PublicationContext == PostPublicationContext.Profile)
            return true;
        if (post.CommunityId == null)
            return true;
        if (post.Community == null)
            return false;
        if (string.Equals(post.Community.Visibility, "public", StringComparison.OrdinalIgnoreCase))
            return true;
        if (viewerUserId == post.UserId)
            return true;
        if (viewerUserId == null)
            return false;
        if (await IsGlobalAdminAsync(viewerUserId.Value, cancellationToken))
            return true;

        return await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(
            viewerUserId.Value,
            post.CommunityId.Value,
            cancellationToken) != null;
    }

    public async Task<bool> CanReadCommunityMembersAsync(
        Community? community,
        int? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        if (community == null)
            return false;
        if (string.Equals(community.Visibility, "public", StringComparison.OrdinalIgnoreCase))
            return true;
        if (viewerUserId == null)
            return false;
        if (community.OwnerUserId == viewerUserId.Value)
            return true;
        if (await IsGlobalAdminAsync(viewerUserId.Value, cancellationToken))
            return true;

        return await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(
            viewerUserId.Value,
            community.Id,
            cancellationToken) != null;
    }

    public async Task<bool> CanEditPostAsync(Post post, int actorUserId, CancellationToken cancellationToken = default) =>
        post.UserId == actorUserId || await IsGlobalAdminAsync(actorUserId, cancellationToken);

    public Task<bool> CanDeletePostAsync(Post post, int actorUserId, CancellationToken cancellationToken = default) =>
        CanEditPostAsync(post, actorUserId, cancellationToken);

    public async Task<bool> CanModeratePostCommentsAsync(
        Post post,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (post.UserId == actorUserId)
            return true;
        if (await IsGlobalAdminAsync(actorUserId, cancellationToken))
            return true;
        if (post.PublicationContext != PostPublicationContext.Community || post.CommunityId == null)
            return false;

        var membership = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(
            actorUserId,
            post.CommunityId.Value,
            cancellationToken);
        return string.Equals(membership?.Role, "owner", StringComparison.OrdinalIgnoreCase)
               || string.Equals(membership?.Role, "admin", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> CanDeleteCommentAsync(
        Comment comment,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (comment.AuthorId == actorUserId)
            return true;
        return await CanModeratePostCommentsAsync(comment.Post, actorUserId, cancellationToken);
    }

    private async Task<bool> IsGlobalAdminAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdNoTrackingAsync(userId, cancellationToken);
        return string.Equals(user?.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
