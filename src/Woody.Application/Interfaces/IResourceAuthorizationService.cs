using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IResourceAuthorizationService
{
    Task<bool> CanReadPostAsync(Post? post, int? viewerUserId, CancellationToken cancellationToken = default);
    Task<bool> CanReadCommunityMembersAsync(Community? community, int? viewerUserId, CancellationToken cancellationToken = default);
    Task<bool> CanEditPostAsync(Post post, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> CanDeletePostAsync(Post post, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> CanModeratePostCommentsAsync(Post post, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> CanDeleteCommentAsync(Comment comment, int actorUserId, CancellationToken cancellationToken = default);
}
