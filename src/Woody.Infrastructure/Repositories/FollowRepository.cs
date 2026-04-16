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

    public Task<int> CountFollowersAsync(int followedUserId, CancellationToken cancellationToken = default) =>
        _db.Follows.AsNoTracking()
            .CountAsync(f => f.FollowedUserId == followedUserId, cancellationToken);

    public Task<int> CountFollowingAsync(int followingUserId, CancellationToken cancellationToken = default) =>
        _db.Follows.AsNoTracking()
            .CountAsync(f => f.FollowingUserId == followingUserId, cancellationToken);

    public async Task<(List<User> Items, int Total)> ListFollowersPagedAsync(
        int followedUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Follows.AsNoTracking()
            .Where(f => f.FollowedUserId == followedUserId);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Include(f => f.FollowingUser).ThenInclude(u => u.Subscription)
            .OrderBy(f => f.FollowingUser.DisplayName ?? f.FollowingUser.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.FollowingUser)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<User> Items, int Total)> ListFollowingPagedAsync(
        int followingUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Follows.AsNoTracking()
            .Where(f => f.FollowingUserId == followingUserId);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Include(f => f.FollowedUser).ThenInclude(u => u.Subscription)
            .OrderBy(f => f.FollowedUser.DisplayName ?? f.FollowedUser.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.FollowedUser)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(Follow follow) => _db.Follows.Add(follow);

    public async Task<Follow?> GetAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default) =>
        await _db.Follows.FirstOrDefaultAsync(
            f => f.FollowingUserId == followingUserId && f.FollowedUserId == followedUserId,
            cancellationToken);

    public void Remove(Follow follow) => _db.Follows.Remove(follow);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
