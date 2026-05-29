using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class UserBlockRepository : IUserBlockRepository
{
    private readonly WoodyDbContext _db;

    public UserBlockRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default) =>
        _db.UserBlocks.AsNoTracking()
            .AnyAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);

    public Task<bool> AreBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default) =>
        _db.UserBlocks.AsNoTracking()
            .AnyAsync(
                b => (b.BlockerUserId == userIdA && b.BlockedUserId == userIdB)
                     || (b.BlockerUserId == userIdB && b.BlockedUserId == userIdA),
                cancellationToken);

    public async Task<HashSet<int>> GetHiddenUserIdsForViewerAsync(int viewerId, CancellationToken cancellationToken = default)
    {
        var blockedByMe = await _db.UserBlocks.AsNoTracking()
            .Where(b => b.BlockerUserId == viewerId)
            .Select(b => b.BlockedUserId)
            .ToListAsync(cancellationToken);

        var blockedMe = await _db.UserBlocks.AsNoTracking()
            .Where(b => b.BlockedUserId == viewerId)
            .Select(b => b.BlockerUserId)
            .ToListAsync(cancellationToken);

        var hidden = new HashSet<int>(blockedByMe);
        foreach (var id in blockedMe)
            hidden.Add(id);

        return hidden;
    }

    public async Task<(List<User> Items, int Total)> ListBlockedUsersPagedAsync(
        int blockerUserId,
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.UserBlocks.AsNoTracking()
            .Where(b => b.BlockerUserId == blockerUserId);

        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(b =>
                b.BlockedUser.Username.ToLower().Contains(search)
                || (b.BlockedUser.DisplayName != null
                    && b.BlockedUser.DisplayName.ToLower().Contains(search)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Include(b => b.BlockedUser).ThenInclude(u => u.Subscription)
            .OrderBy(b => b.BlockedUser.DisplayName ?? b.BlockedUser.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.BlockedUser)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(UserBlock block) => _db.UserBlocks.Add(block);

    public Task<UserBlock?> GetAsync(int blockerUserId, int blockedUserId, CancellationToken cancellationToken = default) =>
        _db.UserBlocks.FirstOrDefaultAsync(
            b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId,
            cancellationToken);

    public void Remove(UserBlock block) => _db.UserBlocks.Remove(block);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
