using Microsoft.EntityFrameworkCore;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
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
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

    public async Task<List<PostFeedCandidate>> ListNonDeletedVisibleFeedCandidatesAsync(
        int? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Posts.AsNoTracking()
            .Where(p => p.DeletedAt == null)
            .Where(p =>
                p.PublicationContext == PostPublicationContext.Profile
                || (p.Community != null && p.Community.Visibility == "public")
                || (viewerUserId != null
                    && p.CommunityId != null
                    && _db.CommunityMemberships.Any(m =>
                        m.UserId == viewerUserId.Value
                        && m.CommunityId == p.CommunityId
                        && m.Status == "active")));

        return await q
            .Select(p => new PostFeedCandidate(
                p.Id,
                p.UserId,
                p.PublicationContext,
                p.CommunityId,
                p.CreatedAt,
                p.Community != null ? p.Community.MemberCount : 0))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Post>> ListNonDeletedByIdsWithNavOrderedAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return new List<Post>();

        var idSet = ids.ToHashSet();
        var rows = await _db.Posts.AsNoTracking()
            .Where(p => idSet.Contains(p.Id) && p.DeletedAt == null)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

        var index = new Dictionary<int, int>(ids.Count);
        for (var i = 0; i < ids.Count; i++)
            index[ids[i]] = i;

        return rows
            .OrderBy(p => index[p.Id])
            .ToList();
    }

    public async Task<(List<Post> Items, int Total)> ListByUserIdPagedAsync(
        int profileUserId,
        int? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Posts.AsNoTracking()
            .Where(p => p.UserId == profileUserId && p.DeletedAt == null)
            .Where(p =>
                p.PublicationContext == PostPublicationContext.Profile
                || (viewerUserId.HasValue && viewerUserId.Value == profileUserId)
                || (p.Community != null && p.Community.Visibility == "public")
                || (viewerUserId != null && p.CommunityId != null && _db.CommunityMemberships.Any(m =>
                    m.UserId == viewerUserId.Value && m.CommunityId == p.CommunityId && m.Status == "active")))
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .Include(p => p.Images);

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
            .Where(p =>
                p.CommunityId == communityId
                && p.DeletedAt == null
                && p.PublicationContext == PostPublicationContext.Community)
            .Include(p => p.User)
            .Include(p => p.Community)
            .Include(p => p.Tags)
            .Include(p => p.Images)
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
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);

    public async Task<Post?> GetByIdTrackedWithTagsAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Post?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Post?> GetByIdNonDeletedForCommentLookupAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Posts.AsNoTracking()
            .Include(p => p.Community)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);

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
            .Include(p => p.Images)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddPostImagesAsync(IEnumerable<PostImage> images, CancellationToken cancellationToken = default)
    {
        await _db.PostImages.AddRangeAsync(images, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
