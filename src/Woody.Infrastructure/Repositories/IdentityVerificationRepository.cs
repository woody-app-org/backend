using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class IdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly WoodyDbContext _context;

    public IdentityVerificationRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task<IdentityVerification?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        => _context.IdentityVerifications
            .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

    public async Task AddAsync(IdentityVerification verification, CancellationToken cancellationToken = default)
        => await _context.IdentityVerifications.AddAsync(verification, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
