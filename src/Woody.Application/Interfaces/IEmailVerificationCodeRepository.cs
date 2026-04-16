using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IEmailVerificationCodeRepository
{
    Task AddAsync(EmailVerificationCode code, CancellationToken cancellationToken = default);
    Task<EmailVerificationCode?> GetLatestByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task InvalidateActiveByUserIdAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
