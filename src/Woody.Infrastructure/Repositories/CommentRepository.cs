using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly WoodyDbContext _db;

    public CommentRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<int, int>> GetActiveCommentCountsByPostIdsAsync(IReadOnlyList<int> postIds, CancellationToken cancellationToken = default)
    {
        if (postIds.Count == 0)
            return new Dictionary<int, int>();

        return await _db.Comments.AsNoTracking()
            .Where(c => postIds.Contains(c.PostId) && c.DeletedAt == null)
            .GroupBy(c => c.PostId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);
    }

    public async Task<List<Comment>> ListActiveForPostWithAuthorAsync(int postId, CancellationToken cancellationToken = default) =>
        await _db.Comments.AsNoTracking()
            .Where(c => c.PostId == postId && c.DeletedAt == null)
            .Include(c => c.Author).ThenInclude(a => a.Subscription)
            .OrderByDescending(c => c.PinnedOnPostAt != null)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Comment?> GetTrackedWithAuthorAsync(int commentId, int postId, CancellationToken cancellationToken = default) =>
        await _db.Comments.Include(c => c.Author).ThenInclude(a => a.Subscription).FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId, cancellationToken);

    public async Task<Comment?> GetTrackedAsync(int commentId, CancellationToken cancellationToken = default) =>
        await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

    public async Task<Comment?> GetTrackedWithPostAsync(int commentId, CancellationToken cancellationToken = default) =>
        await _db.Comments
            .Include(c => c.Post).ThenInclude(p => p.Community)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

    public void Add(Comment comment) => _db.Comments.Add(comment);

    public async Task<Comment?> GetByIdNonDeletedWithAuthorAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Comments.AsNoTracking()
            .Include(c => c.Author).ThenInclude(a => a.Subscription)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);

    public async Task<Comment?> GetTrackedPinnedCommentForPostAsync(int postId, CancellationToken cancellationToken = default) =>
        await _db.Comments
            .Where(c => c.PostId == postId && c.PinnedOnPostAt != null && c.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
