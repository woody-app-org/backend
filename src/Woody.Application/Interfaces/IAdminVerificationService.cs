using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Interfaces;

public interface IAdminVerificationService
{
    /// <summary>Lista solicitações paginadas com filtros opcionais.</summary>
    Task<PaginatedResponseDto<AdminVerificationListItemDto>> ListAsync(
        VerificationStatus? statusFilter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna detalhe de uma solicitação. Null se não encontrada.</summary>
    Task<AdminVerificationDetailDto?> GetDetailAsync(int verificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre o documento para leitura por stream.
    /// Retorna null se a solicitação não existir ou não tiver documento.
    /// </summary>
    Task<VerificationDocumentReadResult?> OpenDocumentStreamAsync(int verificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aprova a solicitação: Status → Approved, deleta documento do storage.
    /// Lança InvalidOperationException se não estiver PendingReview.
    /// </summary>
    Task<AdminVerificationDetailDto> ApproveAsync(int verificationId, int reviewerUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recusa a solicitação: Status → Rejected, registra motivo, deleta documento do storage.
    /// Lança InvalidOperationException se não estiver PendingReview.
    /// </summary>
    Task<AdminVerificationDetailDto> RejectAsync(int verificationId, int reviewerUserId, string rejectionReason, CancellationToken cancellationToken = default);
}
