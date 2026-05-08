using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IIdentityVerificationRepository
{
    Task<IdentityVerification?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IdentityVerification?> GetByUserIdTrackedAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(IdentityVerification verification, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
