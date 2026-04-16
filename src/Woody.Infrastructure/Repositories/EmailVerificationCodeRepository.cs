using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
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

    public async Task<EmailVerificationCode?> GetLatestByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.EmailVerificationCodes
            .Where(x => x.Email == email)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task InvalidateActiveByEmailAsync(string email, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var activeCodes = await _context.EmailVerificationCodes
            .Where(x => x.Email == email
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

    public async Task<bool> HasConsumedCodeForEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.EmailVerificationCodes
            .AnyAsync(x => x.Email == email && x.ConsumedAt != null, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
