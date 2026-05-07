using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class BetaInviteRepository : IBetaInviteRepository
{
    private readonly WoodyDbContext _context;

    public BetaInviteRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsValidForPreviewAsync(string normalizedCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(normalizedCode))
            return Task.FromResult(false);

        var now = DateTime.UtcNow;
        return _context.BetaInvites.AsNoTracking()
            .AnyAsync(
                i => i.Code == normalizedCode
                     && i.IsActive
                     && i.UsesCount < i.MaxUses
                     && (i.ExpiresAt == null || i.ExpiresAt > now),
                cancellationToken);
    }

    public async Task<int?> TryConsumeOneUseAsync(string normalizedCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(normalizedCode))
            return null;

        var now = DateTime.UtcNow;

        var affected = await _context.BetaInvites
            .Where(i => i.Code == normalizedCode
                        && i.IsActive
                        && i.UsesCount < i.MaxUses
                        && (i.ExpiresAt == null || i.ExpiresAt > now))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(i => i.UsesCount, i => i.UsesCount + 1),
                cancellationToken);

        if (affected != 1)
            return null;

        return await _context.BetaInvites.AsNoTracking()
            .Where(i => i.Code == normalizedCode)
            .Select(i => i.Id)
            .FirstAsync(cancellationToken);
    }
}
