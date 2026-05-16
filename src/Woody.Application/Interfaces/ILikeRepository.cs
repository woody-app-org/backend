using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface ILikeRepository
{
    Task<Dictionary<int, int>> GetPostLikeCountsAsync(IReadOnlyList<int> postIds, CancellationToken cancellationToken = default);
    Task<HashSet<int>> GetLikedPostIdsForViewerAsync(int viewerUserId, IReadOnlyList<int> postIds, CancellationToken cancellationToken = default);
    Task<bool> ExistsPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default);
    Task<bool> TryAddPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default);
    void Add(Like like);
    Task<Like?> GetPostLikeAsync(int userId, int postId, CancellationToken cancellationToken = default);
    void Remove(Like like);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<int, int>> GetCommentLikeCountsAsync(
        IReadOnlyList<int> commentIds,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetCommentIdsLikedByUserAsync(
        int userId,
        IReadOnlyList<int> commentIds,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddCommentLikeAsync(int userId, int commentId, CancellationToken cancellationToken = default);

    /// <summary>Remove gosto se existir; idempotente (sem erro se não existir linha).</summary>
    Task RemoveCommentLikeAsync(int userId, int commentId, CancellationToken cancellationToken = default);
}
