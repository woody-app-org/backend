using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface IIdentityVerificationRepository
{
    // ── Acesso por UserId (usuária) ───────────────────────────────────────────
    Task<IdentityVerification?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IdentityVerification?> GetByUserIdTrackedAsync(int userId, CancellationToken cancellationToken = default);

    // ── Acesso por Id (admin) ─────────────────────────────────────────────────
    Task<IdentityVerification?> GetByIdWithUserAsync(int id, CancellationToken cancellationToken = default);
    Task<IdentityVerification?> GetByIdWithUserTrackedAsync(int id, CancellationToken cancellationToken = default);

    // ── Lista paginada para dashboard admin ───────────────────────────────────
    Task<(List<IdentityVerification> Items, int TotalCount)> ListPagedAsync(
        VerificationStatus? statusFilter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // ── Persistência ──────────────────────────────────────────────────────────
    Task AddAsync(IdentityVerification verification, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
