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
}
