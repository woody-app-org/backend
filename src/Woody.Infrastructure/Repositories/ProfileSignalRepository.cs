using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class ProfileSignalRepository : IProfileSignalRepository
{
    private readonly WoodyDbContext _db;

    public ProfileSignalRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileSignal?> GetByIdWithUsersAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.ProfileSignals
            .Include(s => s.SenderUser).ThenInclude(u => u.Subscription)
            .Include(s => s.ReceiverUser).ThenInclude(u => u.Subscription)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<ProfileSignal?> GetLatestOfTypeBetweenAsync(
        int senderUserId,
        int receiverUserId,
        ProfileSignalType type,
        CancellationToken cancellationToken = default) =>
        await _db.ProfileSignals.AsNoTracking()
            .Where(s => s.SenderUserId == senderUserId && s.ReceiverUserId == receiverUserId && s.Type == type)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasSentTypeSinceAsync(
        int senderUserId,
        int receiverUserId,
        ProfileSignalType type,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default) =>
        _db.ProfileSignals.AsNoTracking()
            .AnyAsync(
                s => s.SenderUserId == senderUserId
                    && s.ReceiverUserId == receiverUserId
                    && s.Type == type
                    && s.CreatedAt >= sinceUtc,
                cancellationToken);

    public async Task<(List<ProfileSignal> Items, int Total)> ListReceivedInboxPagedAsync(
        int receiverUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _db.ProfileSignals.AsNoTracking()
            .Where(s => s.ReceiverUserId == receiverUserId
                && (s.Status == ProfileSignalStatus.Sent || s.Status == ProfileSignalStatus.Read));

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Include(s => s.SenderUser).ThenInclude(u => u.Subscription)
            .Include(s => s.ReceiverUser).ThenInclude(u => u.Subscription)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<ProfileSignal> Items, int Total)> ListSentPagedAsync(
        int senderUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _db.ProfileSignals.AsNoTracking()
            .Where(s => s.SenderUserId == senderUserId);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Include(s => s.SenderUser).ThenInclude(u => u.Subscription)
            .Include(s => s.ReceiverUser).ThenInclude(u => u.Subscription)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(ProfileSignal signal) => _db.ProfileSignals.Add(signal);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
