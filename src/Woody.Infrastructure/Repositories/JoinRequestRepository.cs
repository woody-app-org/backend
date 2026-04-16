using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class JoinRequestRepository : IJoinRequestRepository
{
    private readonly WoodyDbContext _db;

    public JoinRequestRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<List<JoinRequest>> ListPendingWithUserForCommunityAsync(int communityId, CancellationToken cancellationToken = default) =>
        await _db.JoinRequests.AsNoTracking()
            .Where(j => j.CommunityId == communityId && j.Status == "pending")
            .Include(j => j.User).ThenInclude(u => u.Subscription)
            .OrderBy(j => j.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<JoinRequest?> GetWithCommunityTrackedAsync(int joinRequestId, CancellationToken cancellationToken = default) =>
        await _db.JoinRequests.Include(j => j.Community).FirstOrDefaultAsync(j => j.Id == joinRequestId, cancellationToken);

    public async Task<JoinRequest?> GetTrackedAsync(int joinRequestId, CancellationToken cancellationToken = default) =>
        await _db.JoinRequests.FirstOrDefaultAsync(j => j.Id == joinRequestId, cancellationToken);

    public Task<bool> ExistsPendingAsync(int communityId, int userId, CancellationToken cancellationToken = default) =>
        _db.JoinRequests.AnyAsync(
            j => j.CommunityId == communityId && j.UserId == userId && j.Status == "pending",
            cancellationToken);

    public void Add(JoinRequest request) => _db.JoinRequests.Add(request);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
