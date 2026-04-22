using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface ICommentRepository
{
    Task<Dictionary<int, int>> GetActiveCommentCountsByPostIdsAsync(IReadOnlyList<int> postIds, CancellationToken cancellationToken = default);
    Task<List<Comment>> ListActiveForPostWithAuthorAsync(int postId, CancellationToken cancellationToken = default);
    Task<Comment?> GetTrackedWithAuthorAsync(int commentId, int postId, CancellationToken cancellationToken = default);
    Task<Comment?> GetTrackedAsync(int commentId, CancellationToken cancellationToken = default);
    void Add(Comment comment);
    Task<Comment?> GetByIdNonDeletedWithAuthorAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Comentário fixo atual do post (tracked), se existir.</summary>
    Task<Comment?> GetTrackedPinnedCommentForPostAsync(int postId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
