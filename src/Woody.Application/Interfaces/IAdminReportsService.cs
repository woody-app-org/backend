using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface IAdminReportsService
{
    Task<PaginatedResponseDto<AdminReportListItemDto>> ListAsync(
        ReportStatus? status,
        string? targetType,
        string? reasonCode,
        string? search,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminReportDetailDto?> GetDetailAsync(int reportId, CancellationToken cancellationToken = default);

    Task<AdminReportDetailDto> UpdateStatusAsync(
        int reportId,
        ReportStatus newStatus,
        int reviewerUserId,
        string? internalNote,
        string? resolutionCode,
        CancellationToken cancellationToken = default);
}
