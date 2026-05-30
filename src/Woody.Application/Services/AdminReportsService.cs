using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class AdminReportsService : IAdminReportsService
{
    private readonly IContentReportRepository _reports;

    public AdminReportsService(IContentReportRepository reports)
    {
        _reports = reports;
    }

    // ── Lista paginada ────────────────────────────────────────────────────────

    public async Task<PaginatedResponseDto<AdminReportListItemDto>> ListAsync(
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
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _reports.ListPagedAsync(
            status, targetType, reasonCode, search,
            dateFrom, dateTo, page, pageSize, cancellationToken);

        var counts = await _reports.CountByTargetsBatchAsync(items, cancellationToken);

        var dtos = items.Select(r =>
        {
            var key = $"{r.TargetType}|{r.PostId}|{r.CommentId}";
            var count = counts.TryGetValue(key, out var c) ? c : 1;
            return ToListItemDto(r, count);
        }).ToList();

        return new PaginatedResponseDto<AdminReportListItemDto>
        {
            Items          = dtos,
            Page           = page,
            PageSize       = pageSize,
            TotalCount     = total,
            HasPreviousPage = page > 1,
            HasNextPage    = (page * pageSize) < total
        };
    }

    // ── Detalhe ───────────────────────────────────────────────────────────────

    public async Task<AdminReportDetailDto?> GetDetailAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        var r = await _reports.GetDetailAsync(reportId, cancellationToken);
        if (r == null) return null;

        var count = await _reports.CountByTargetAsync(
            r.TargetType, r.PostId, r.CommentId, cancellationToken);

        return ToDetailDto(r, count);
    }

    // ── Atualizar status ──────────────────────────────────────────────────────

    public async Task<AdminReportDetailDto> UpdateStatusAsync(
        int reportId,
        ReportStatus newStatus,
        int reviewerUserId,
        string? internalNote,
        string? resolutionCode,
        CancellationToken cancellationToken = default)
    {
        var r = await _reports.GetTrackedAsync(reportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Denúncia {reportId} não encontrada.");

        r.Status          = newStatus;
        r.ReviewedByUserId = reviewerUserId;
        r.ReviewedAt      = DateTime.UtcNow;
        r.UpdatedAt       = DateTime.UtcNow;

        // null = campo não enviado (não toca); string vazia = admin quer limpar a nota
        if (internalNote != null)
            r.InternalNote = string.IsNullOrWhiteSpace(internalNote) ? null : internalNote.Trim();

        if (resolutionCode != null)
            r.ResolutionCode = string.IsNullOrWhiteSpace(resolutionCode) ? null : resolutionCode.Trim();

        await _reports.SaveChangesAsync(cancellationToken);

        var detail = await _reports.GetDetailAsync(reportId, cancellationToken)
            ?? throw new InvalidOperationException("Falha ao recarregar denúncia após atualização.");

        var count = await _reports.CountByTargetAsync(
            detail.TargetType, detail.PostId, detail.CommentId, cancellationToken);

        return ToDetailDto(detail, count);
    }

    // ── Mapeamentos privados ──────────────────────────────────────────────────

    private static AdminReportListItemDto ToListItemDto(ContentReport r, int sameTargetCount)
    {
        var authorUser = r.TargetType == "post"
            ? r.Post?.User
            : r.Comment?.Author;

        var snippet = r.TargetType == "post"
            ? Truncate(r.Post?.Content, 280)
            : Truncate(r.Comment?.Content, 280);

        return new AdminReportListItemDto
        {
            Id                   = r.Id,
            TargetType           = r.TargetType,
            ReasonCode           = r.ReasonCode,
            Status               = r.Status.ToString(),
            ReporterUser         = ToPublicDto(r.Reporter),
            ReportedContentAuthor = authorUser != null ? ToPublicDto(authorUser) : null,
            TargetPreview        = new AdminReportTargetPreviewDto
            {
                PostId          = r.PostId,
                CommentId       = r.CommentId,
                ContentSnippet  = snippet
            },
            SameTargetReportCount = sameTargetCount,
            CreatedAt            = r.CreatedAt,
            UpdatedAt            = r.UpdatedAt
        };
    }

    private static AdminReportDetailDto ToDetailDto(ContentReport r, int sameTargetCount)
    {
        var authorUser = r.TargetType == "post"
            ? r.Post?.User
            : r.Comment?.Author;

        AdminReportPostDetailDto? postDto   = null;
        AdminReportCommentDetailDto? commentDto = null;

        if (r.TargetType == "post" && r.Post != null)
            postDto = ToPostDetailDto(r.Post);

        if (r.TargetType == "comment" && r.Comment != null)
        {
            commentDto = new AdminReportCommentDetailDto
            {
                Id        = r.Comment.Id,
                Content   = r.Comment.Content,
                IsDeleted = r.Comment.DeletedAt.HasValue,
                CreatedAt = r.Comment.CreatedAt,
                ParentPost = r.Comment.Post != null ? ToPostDetailDto(r.Comment.Post) : null
            };
        }

        AdminReportReviewerDto? reviewerDto = null;
        if (r.ReviewedBy != null)
        {
            reviewerDto = new AdminReportReviewerDto
            {
                Id          = r.ReviewedBy.Id,
                Username    = r.ReviewedBy.Username,
                DisplayName = r.ReviewedBy.DisplayName
            };
        }

        return new AdminReportDetailDto
        {
            Id                    = r.Id,
            TargetType            = r.TargetType,
            ReasonCode            = r.ReasonCode,
            Details               = r.Details,
            Status                = r.Status.ToString(),
            InternalNote          = r.InternalNote,
            ResolutionCode        = r.ResolutionCode,
            ReporterUser          = ToPublicDto(r.Reporter),
            ReportedContentAuthor = authorUser != null ? ToPublicDto(authorUser) : null,
            ReviewedBy            = reviewerDto,
            Post                  = postDto,
            Comment               = commentDto,
            SameTargetReportCount = sameTargetCount,
            CreatedAt             = r.CreatedAt,
            UpdatedAt             = r.UpdatedAt,
            ReviewedAt            = r.ReviewedAt
        };
    }

    private static AdminReportPostDetailDto ToPostDetailDto(Post post)
    {
        return new AdminReportPostDetailDto
        {
            Id        = post.Id,
            PublicId  = post.PublicId,
            Content   = post.Content,
            IsDeleted = post.DeletedAt.HasValue,
            CreatedAt = post.CreatedAt,
            Media     = post.MediaAttachments
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new AdminReportMediaItemDto
                {
                    Kind = m.MediaKind.ToString().ToLowerInvariant(),
                    Url  = m.Url
                })
                .ToList()
        };
    }

    private static UserPublicDto ToPublicDto(User u) => new()
    {
        Id       = u.Id.ToString(),
        Name     = u.DisplayName ?? u.Username,
        Username = u.Username,
        AvatarUrl = u.ProfilePic
    };

    private static string? Truncate(string? text, int maxLength)
    {
        if (text == null) return null;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
