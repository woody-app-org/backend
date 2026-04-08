using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class FollowRepository : IFollowRepository
{
    private readonly WoodyDbContext _db;

    public FollowRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default) =>
        _db.Follows.AnyAsync(f => f.FollowingUserId == followingUserId && f.FollowedUserId == followedUserId, cancellationToken);

    public async Task<List<int>> GetFollowedUserIdsAsync(int followingUserId, CancellationToken cancellationToken = default) =>
        await _db.Follows.AsNoTracking()
            .Where(f => f.FollowingUserId == followingUserId)
            .Select(f => f.FollowedUserId)
            .ToListAsync(cancellationToken);

    public async Task<List<Follow>> ListFollowingWithFollowedUserAsync(int followingUserId, CancellationToken cancellationToken = default) =>
        await _db.Follows.AsNoTracking()
            .Where(f => f.FollowingUserId == followingUserId)
            .Include(f => f.FollowedUser)
            .OrderBy(f => f.FollowedUser.DisplayName ?? f.FollowedUser.Username)
            .ToListAsync(cancellationToken);

    public void Add(Follow follow) => _db.Follows.Add(follow);

    public async Task<Follow?> GetAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default) =>
        await _db.Follows.FirstOrDefaultAsync(
            f => f.FollowingUserId == followingUserId && f.FollowedUserId == followedUserId,
            cancellationToken);

    public void Remove(Follow follow) => _db.Follows.Remove(follow);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
