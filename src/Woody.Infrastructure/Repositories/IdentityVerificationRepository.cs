using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class IdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly WoodyDbContext _context;

    public IdentityVerificationRepository(WoodyDbContext context)
    {
        _context = context;
    }

    // ── Por UserId (usuária) ──────────────────────────────────────────────────

    public Task<IdentityVerification?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        => _context.IdentityVerifications
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

    public Task<IdentityVerification?> GetByUserIdTrackedAsync(int userId, CancellationToken cancellationToken = default)
        => _context.IdentityVerifications
            .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

    // ── Por Id com User (admin) ───────────────────────────────────────────────

    public Task<IdentityVerification?> GetByIdWithUserAsync(int id, CancellationToken cancellationToken = default)
        => _context.IdentityVerifications
            .AsNoTracking()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<IdentityVerification?> GetByIdWithUserTrackedAsync(int id, CancellationToken cancellationToken = default)
        => _context.IdentityVerifications
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    // ── Lista paginada para dashboard admin ───────────────────────────────────

    public async Task<(List<IdentityVerification> Items, int TotalCount)> ListPagedAsync(
        VerificationStatus? statusFilter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.IdentityVerifications
            .AsNoTracking()
            .Include(v => v.User)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(v => v.Status == statusFilter.Value);

        if (dateFrom.HasValue)
            query = query.Where(v => v.DocumentSubmittedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(v => v.DocumentSubmittedAt <= dateTo.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(v => v.DocumentSubmittedAt ?? v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // ── Persistência ──────────────────────────────────────────────────────────

    public async Task AddAsync(IdentityVerification verification, CancellationToken cancellationToken = default)
        => await _context.IdentityVerifications.AddAsync(verification, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
