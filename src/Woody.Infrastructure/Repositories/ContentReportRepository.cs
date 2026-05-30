using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class ContentReportRepository : IContentReportRepository
{
    private readonly WoodyDbContext _db;

    public ContentReportRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public void Add(ContentReport report) => _db.ContentReports.Add(report);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<(List<ContentReport> Items, int TotalCount)> ListPagedAsync(
        ReportStatus? status,
        string? targetType,
        string? reasonCode,
        string? search,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ContentReports
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.Post).ThenInclude(p => p != null ? p.User : null)
            .Include(r => r.Comment).ThenInclude(c => c != null ? c.Author : null)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(r => r.TargetType == targetType.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(reasonCode))
            query = query.Where(r => r.ReasonCode == reasonCode);

        if (dateFrom.HasValue)
            query = query.Where(r => r.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.CreatedAt <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.Reporter.Username.ToLower().Contains(term) ||
                (r.Reporter.DisplayName != null && r.Reporter.DisplayName.ToLower().Contains(term)) ||
                (r.Post != null && (
                    r.Post.User.Username.ToLower().Contains(term) ||
                    (r.Post.User.DisplayName != null && r.Post.User.DisplayName.ToLower().Contains(term)))) ||
                (r.Comment != null && (
                    r.Comment.Author.Username.ToLower().Contains(term) ||
                    (r.Comment.Author.DisplayName != null && r.Comment.Author.DisplayName.ToLower().Contains(term)))));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ContentReport?> GetDetailAsync(int reportId, CancellationToken cancellationToken = default)
    {
        return await _db.ContentReports
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.ReviewedBy)
            .Include(r => r.Post)
                .ThenInclude(p => p != null ? p.User : null)
            .Include(r => r.Post)
                .ThenInclude(p => p != null ? p.MediaAttachments : null)
            .Include(r => r.Comment)
                .ThenInclude(c => c != null ? c.Author : null)
            .Include(r => r.Comment)
                .ThenInclude(c => c != null ? c.Post : null)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    public async Task<ContentReport?> GetTrackedAsync(int reportId, CancellationToken cancellationToken = default)
    {
        return await _db.ContentReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    public async Task<int> CountByTargetAsync(
        string targetType,
        int? postId,
        int? commentId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ContentReports
            .AsNoTracking()
            .Where(r => r.TargetType == targetType &&
                        r.PostId == postId &&
                        r.CommentId == commentId)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> CountByTargetsBatchAsync(
        IEnumerable<ContentReport> reports,
        CancellationToken cancellationToken = default)
    {
        // Extrai combinações únicas (targetType, postId, commentId) para uma única query
        var targets = reports
            .Select(r => new { r.TargetType, r.PostId, r.CommentId })
            .Distinct()
            .ToList();

        if (targets.Count == 0)
            return [];

        // Uma query com GROUP BY; WHERE usa EF Core value-comparison em memória após fetch
        // Para conjuntos pequenos (≤50) isso é eficiente o suficiente
        var postIds     = targets.Where(t => t.PostId.HasValue).Select(t => t.PostId!.Value).ToHashSet();
        var commentIds  = targets.Where(t => t.CommentId.HasValue).Select(t => t.CommentId!.Value).ToHashSet();

        var rows = await _db.ContentReports
            .AsNoTracking()
            .Where(r =>
                (r.PostId == null    || postIds.Contains(r.PostId.Value)) &&
                (r.CommentId == null || commentIds.Contains(r.CommentId.Value)))
            .GroupBy(r => new { r.TargetType, r.PostId, r.CommentId })
            .Select(g => new { g.Key.TargetType, g.Key.PostId, g.Key.CommentId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => $"{r.TargetType}|{r.PostId}|{r.CommentId}",
            r => r.Count);
    }
}
