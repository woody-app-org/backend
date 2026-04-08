using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly WoodyDbContext _db;

    public PostRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<List<Post>> ListNonDeletedWithNavAsync(CancellationToken cancellationToken = default) =>
        await _db.Posts.AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .ToListAsync(cancellationToken);

    public async Task<(List<Post> Items, int Total)> ListByUserIdPagedAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _db.Posts.AsNoTracking()
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(List<Post> Items, int Total)> ListByCommunityIdPagedAsync(
        int communityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _db.Posts.AsNoTracking()
            .Where(p => p.CommunityId == communityId && p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .OrderByDescending(p => p.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<Post?> GetByIdNonDeletedWithNavAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);

    public async Task<Post?> GetByIdTrackedWithTagsAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Post?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Post?> GetByIdNonDeletedForCommentLookupAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Post post) => _db.Posts.Add(post);

    public async Task AddPostTagsAsync(IEnumerable<PostTag> tags, CancellationToken cancellationToken = default)
    {
        await _db.PostTags.AddRangeAsync(tags, cancellationToken);
    }

    public void RemovePostTags(IEnumerable<PostTag> tags) => _db.PostTags.RemoveRange(tags);

    public async Task<List<Post>> SearchNonDeletedWithNavAsync(string loweredQuery, int take, CancellationToken cancellationToken = default) =>
        await _db.Posts.AsNoTracking()
            .Where(p => p.DeletedAt == null && (p.Title.ToLower().Contains(loweredQuery) || p.Content.ToLower().Contains(loweredQuery)))
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
