using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class RefreshTokenSessionRepository : IRefreshTokenSessionRepository
{
    private readonly WoodyDbContext _context;

    public RefreshTokenSessionRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken = default) =>
        _context.RefreshTokenSessions.AddAsync(session, cancellationToken).AsTask();

    public Task<RefreshTokenSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.RefreshTokenSessions.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeActiveForUserAsync(
        int userId,
        DateTime utcNow,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _context.RefreshTokenSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.RevokedAt = utcNow;
            session.RevocationReason = reason;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
