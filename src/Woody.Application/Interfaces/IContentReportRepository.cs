using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface IContentReportRepository
{
    void Add(ContentReport report);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // ── Consultas administrativas ─────────────────────────────────────────────

    Task<(List<ContentReport> Items, int TotalCount)> ListPagedAsync(
        ReportStatus? status,
        string? targetType,
        string? reasonCode,
        string? search,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ContentReport?> GetDetailAsync(int reportId, CancellationToken cancellationToken = default);

    Task<ContentReport?> GetTrackedAsync(int reportId, CancellationToken cancellationToken = default);

    Task<int> CountByTargetAsync(
        string targetType,
        int? postId,
        int? commentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a contagem de denúncias agrupada por (TargetType, PostId, CommentId)
    /// para uma lista de denúncias, evitando N+1 queries na listagem.
    /// Chave: "targetType|postId|commentId"
    /// </summary>
    Task<Dictionary<string, int>> CountByTargetsBatchAsync(
        IEnumerable<ContentReport> reports,
        CancellationToken cancellationToken = default);
}
