using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class PasswordResetSessionRepository : IPasswordResetSessionRepository
{
    private readonly WoodyDbContext _context;

    public PasswordResetSessionRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(PasswordResetSession session, CancellationToken cancellationToken = default) =>
        _context.PasswordResetSessions.AddAsync(session, cancellationToken).AsTask();

    public Task<PasswordResetSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.PasswordResetSessions.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task InvalidateActiveForUserAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.PasswordResetSessions
            .Where(x => x.UserId == userId && x.ConsumedAt == null && x.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
            session.ConsumedAt = utcNow;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
