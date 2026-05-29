using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class EmailVerificationCodeRepository : IEmailVerificationCodeRepository
{
    private readonly WoodyDbContext _context;

    public EmailVerificationCodeRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailVerificationCode code, CancellationToken cancellationToken = default) =>
        await _context.EmailVerificationCodes.AddAsync(code, cancellationToken);

    public async Task<EmailVerificationCode?> GetLatestByEmailAndPurposeAsync(
        string email,
        VerificationCodePurpose purpose,
        CancellationToken cancellationToken = default) =>
        await _context.EmailVerificationCodes
            .Where(x => x.Email == email && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task InvalidateActiveByEmailAndPurposeAsync(
        string email,
        VerificationCodePurpose purpose,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var activeCodes = await _context.EmailVerificationCodes
            .Where(x => x.Email == email
                        && x.Purpose == purpose
                        && x.ConsumedAt == null
                        && x.InvalidatedAt == null
                        && x.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var code in activeCodes)
        {
            code.InvalidatedAt = utcNow;
            code.UpdatedAt = utcNow;
        }
    }

    public async Task<bool> HasConsumedCodeForEmailAndPurposeAsync(
        string email,
        VerificationCodePurpose purpose,
        CancellationToken cancellationToken = default) =>
        await _context.EmailVerificationCodes
            .AnyAsync(
                x => x.Email == email && x.Purpose == purpose && x.ConsumedAt != null,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
