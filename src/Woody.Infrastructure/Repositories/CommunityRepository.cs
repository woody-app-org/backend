using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommunityRepository : ICommunityRepository
{
    private readonly WoodyDbContext _db;

    public CommunityRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsNoTrackingAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Communities.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<List<Community>> ListWithTagsOrderedByNameAsync(CancellationToken cancellationToken = default) =>
        await _db.Communities.AsNoTracking()
            .Include(c => c.Tags)
            .Include(c => c.Subscription)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<Community?> GetByIdWithTagsNoTrackingAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Communities.AsNoTracking()
            .Include(x => x.Tags)
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Community?> GetBySlugWithTagsNoTrackingAsync(string slug, CancellationToken cancellationToken = default) =>
        await _db.Communities.AsNoTracking()
            .Include(x => x.Tags)
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public async Task<Community?> GetBySlugTrackedAsync(string slug, CancellationToken cancellationToken = default) =>
        await _db.Communities.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    public async Task<Community?> GetByIdTrackedWithTagsAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Communities.Include(x => x.Tags).Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Community?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Communities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Community>> SearchWithTagsAsync(string loweredQuery, int take, CancellationToken cancellationToken = default) =>
        await _db.Communities.AsNoTracking()
            .Include(c => c.Tags)
            .Include(c => c.Subscription)
            .Where(c => c.Name.ToLower().Contains(loweredQuery) || c.Slug.ToLower().Contains(loweredQuery) || c.Description.ToLower().Contains(loweredQuery))
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsSlugNoTrackingAsync(string slug, CancellationToken cancellationToken = default) =>
        _db.Communities.AsNoTracking().AnyAsync(c => c.Slug == slug, cancellationToken);

    public void Add(Community community) => _db.Communities.Add(community);

    public void RemoveCommunityTags(IEnumerable<CommunityTag> tags) => _db.CommunityTags.RemoveRange(tags);

    public void AddCommunityTag(CommunityTag tag) => _db.CommunityTags.Add(tag);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
