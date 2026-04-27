using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface ILoginLockoutRepository
{
    Task<LoginLockout?> GetByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default);
    void Add(LoginLockout lockout);
    void Remove(LoginLockout lockout);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
