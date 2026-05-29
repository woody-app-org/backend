using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IPasswordResetSessionRepository
{
    Task AddAsync(PasswordResetSession session, CancellationToken cancellationToken = default);

    Task<PasswordResetSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task InvalidateActiveForUserAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
