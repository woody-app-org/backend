using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface IEmailVerificationCodeRepository
{
    Task AddAsync(EmailVerificationCode code, CancellationToken cancellationToken = default);

    Task<EmailVerificationCode?> GetLatestByEmailAndPurposeAsync(
        string email,
        VerificationCodePurpose purpose,
        CancellationToken cancellationToken = default);

    Task InvalidateActiveByEmailAndPurposeAsync(
        string email,
        VerificationCodePurpose purpose,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> HasConsumedCodeForEmailAndPurposeAsync(
        string email,
        VerificationCodePurpose purpose,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
