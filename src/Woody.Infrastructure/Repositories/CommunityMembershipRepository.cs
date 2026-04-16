using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommunityMembershipRepository : ICommunityMembershipRepository
{
    private readonly WoodyDbContext _db;

    public CommunityMembershipRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<List<string>> GetActiveCommunityIdsAsStringsAsync(int userId, CancellationToken cancellationToken = default) =>
        await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == "active")
            .Select(m => m.CommunityId.ToString())
            .ToListAsync(cancellationToken);

    public async Task<List<CommunityMembership>> ListActiveWithCommunityAndTagsByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == "active")
            .Include(m => m.Community)
            .ThenInclude(c => c!.Tags)
            .ToListAsync(cancellationToken);

    public async Task<CommunityMembership?> GetForUserAndCommunityAsync(int userId, int communityId, CancellationToken cancellationToken = default) =>
        await _db.CommunityMemberships.FirstOrDefaultAsync(
            x => x.CommunityId == communityId && x.UserId == userId,
            cancellationToken);

    public async Task<CommunityMembership?> GetActiveForUserAndCommunityNoTrackingAsync(int userId, int communityId, CancellationToken cancellationToken = default) =>
        await _db.CommunityMemberships.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.CommunityId == communityId && m.UserId == userId && m.Status == "active",
                cancellationToken);

    public async Task<(List<CommunityMembership> Rows, int Total)> ListActiveMembersPagedOrderedAsync(
        int communityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.CommunityId == communityId && m.Status == "active")
            .Include(m => m.User).ThenInclude(u => u.Subscription)
            .OrderBy(m => m.Role == "owner" ? 0 : m.Role == "admin" ? 1 : 2)
            .ThenBy(m => m.User.DisplayName ?? m.User.Username)
            .ThenBy(m => m.User.Username);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (rows, total);
    }

    public Task<int> CountActiveInCommunityAsync(int communityId, CancellationToken cancellationToken = default) =>
        _db.CommunityMemberships.CountAsync(x => x.CommunityId == communityId && x.Status == "active", cancellationToken);

    public void Add(CommunityMembership membership) => _db.CommunityMemberships.Add(membership);

    public void Remove(CommunityMembership membership) => _db.CommunityMemberships.Remove(membership);

    public async Task<List<int>> GetActiveCommunityIdsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await _db.CommunityMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == "active")
            .Select(m => m.CommunityId)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForUserAndCommunityAsync(int userId, int communityId, CancellationToken cancellationToken = default) =>
        _db.CommunityMemberships.AnyAsync(m => m.CommunityId == communityId && m.UserId == userId, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
