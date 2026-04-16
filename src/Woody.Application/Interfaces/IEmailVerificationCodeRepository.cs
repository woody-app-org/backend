using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IEmailVerificationCodeRepository
{
    Task AddAsync(EmailVerificationCode code, CancellationToken cancellationToken = default);
    Task<EmailVerificationCode?> GetLatestByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task InvalidateActiveByEmailAsync(string email, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<bool> HasConsumedCodeForEmailAsync(string email, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
