using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IRefreshTokenSessionRepository
{
    Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken = default);
    Task<RefreshTokenSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeActiveForUserAsync(int userId, DateTime utcNow, string reason, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
