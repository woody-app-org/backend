using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Application.Helpers;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class AdminVerificationService : IAdminVerificationService
{
    private readonly IIdentityVerificationRepository _verifications;
    private readonly IUserRepository _users;
    private readonly IVerificationDocumentStorageProvider _storage;

    public AdminVerificationService(
        IIdentityVerificationRepository verifications,
        IUserRepository users,
        IVerificationDocumentStorageProvider storage)
    {
        _verifications = verifications;
        _users = users;
        _storage = storage;
    }

    // ── Lista paginada ────────────────────────────────────────────────────────

    public async Task<PaginatedResponseDto<AdminVerificationListItemDto>> ListAsync(
        VerificationStatus? statusFilter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _verifications.ListPagedAsync(
            statusFilter, dateFrom, dateTo, page, pageSize, cancellationToken);

        return new PaginatedResponseDto<AdminVerificationListItemDto>
        {
            Items = items.Select(ToListItemDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            HasPreviousPage = page > 1,
            HasNextPage = (page * pageSize) < total
        };
    }

    // ── Detalhe ───────────────────────────────────────────────────────────────

    public async Task<AdminVerificationDetailDto?> GetDetailAsync(
        int verificationId,
        CancellationToken cancellationToken = default)
    {
        var v = await _verifications.GetByIdWithUserAsync(verificationId, cancellationToken);
        return v == null ? null : ToDetailDto(v);
    }

    // ── Stream do documento (para proxy) ─────────────────────────────────────

    public async Task<VerificationDocumentReadResult?> OpenDocumentStreamAsync(
        int verificationId,
        CancellationToken cancellationToken = default)
    {
        var v = await _verifications.GetByIdWithUserAsync(verificationId, cancellationToken);

        if (v == null || string.IsNullOrEmpty(v.DocumentStorageKey))
            return null;

        return await _storage.OpenReadAsync(v.DocumentStorageKey, cancellationToken);
    }

    // ── Aprovar ───────────────────────────────────────────────────────────────

    public async Task<AdminVerificationDetailDto> ApproveAsync(
        int verificationId,
        int reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        var v = await _verifications.GetByIdWithUserTrackedAsync(verificationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Solicitação {verificationId} não encontrada.");

        if (v.Status != VerificationStatus.PendingReview)
            throw new InvalidOperationException(
                $"Somente solicitações PendingReview podem ser aprovadas. Status atual: {v.Status}");

        var now = DateTime.UtcNow;

        // ── Deletar documento do storage após decisão ─────────────────────────
        await DeleteDocumentSilentlyAsync(v, now, cancellationToken);

        // ── Atualizar IdentityVerification ────────────────────────────────────
        v.Status = VerificationStatus.Approved;
        v.ReviewedByUserId = reviewerUserId;
        v.ReviewedAt = now;
        v.RejectionReason = null;
        v.UpdatedAt = now;
        v.DecisionLog = VerificationDecisionLogHelper.Append(v.DecisionLog, "approved", reviewerUserId, now);

        // ── Atualizar User ────────────────────────────────────────────────────
        v.User.VerificationStatus = VerificationStatus.Approved;
        v.User.UpdatedAt = now;

        await _verifications.SaveChangesAsync(cancellationToken);

        return ToDetailDto(v);
    }

    // ── Recusar ───────────────────────────────────────────────────────────────

    public async Task<AdminVerificationDetailDto> RejectAsync(
        int verificationId,
        int reviewerUserId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason) || rejectionReason.Length < 10)
            throw new ArgumentException("O motivo de recusa deve ter ao menos 10 caracteres.");

        if (rejectionReason.Length > 500)
            throw new ArgumentException("O motivo de recusa deve ter no máximo 500 caracteres.");

        var v = await _verifications.GetByIdWithUserTrackedAsync(verificationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Solicitação {verificationId} não encontrada.");

        if (v.Status != VerificationStatus.PendingReview)
            throw new InvalidOperationException(
                $"Somente solicitações PendingReview podem ser recusadas. Status atual: {v.Status}");

        var now = DateTime.UtcNow;

        // ── Deletar documento do storage após decisão ─────────────────────────
        await DeleteDocumentSilentlyAsync(v, now, cancellationToken);

        // ── Atualizar IdentityVerification ────────────────────────────────────
        v.Status = VerificationStatus.Rejected;
        v.ReviewedByUserId = reviewerUserId;
        v.ReviewedAt = now;
        v.RejectionReason = rejectionReason.Trim();
        v.UpdatedAt = now;
        v.DecisionLog = VerificationDecisionLogHelper.Append(v.DecisionLog, "rejected", reviewerUserId, now);

        // ── Atualizar User ────────────────────────────────────────────────────
        v.User.VerificationStatus = VerificationStatus.Rejected;
        v.User.UpdatedAt = now;

        await _verifications.SaveChangesAsync(cancellationToken);

        return ToDetailDto(v);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task DeleteDocumentSilentlyAsync(
        IdentityVerification v,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(v.DocumentStorageKey))
            return;

        try
        {
            await _storage.DeleteAsync(v.DocumentStorageKey, cancellationToken);
        }
        catch
        {
            // Não bloquear a decisão caso o arquivo já não exista no storage
        }

        v.DocumentStorageKey = null;
        v.DocumentDeletedAt = now;
    }

    private static AdminVerificationListItemDto ToListItemDto(IdentityVerification v) => new()
    {
        VerificationId = v.Id,
        UserId = v.UserId,
        Username = v.User.Username,
        DisplayName = v.User.DisplayName,
        Email = v.User.Email,
        AvatarUrl = v.User.ProfilePic,
        Status = v.Status.ToString(),
        DocumentSubmittedAt = v.DocumentSubmittedAt?.ToUniversalTime().ToString("o"),
        AttemptCount = v.AttemptCount,
        ReviewedAt = v.ReviewedAt?.ToUniversalTime().ToString("o"),
        RejectionReasonSummary = v.Status == VerificationStatus.Rejected && v.RejectionReason != null
            ? TruncateSummary(v.RejectionReason, 80)
            : null
    };

    private static AdminVerificationDetailDto ToDetailDto(IdentityVerification v) => new()
    {
        VerificationId = v.Id,
        UserId = v.UserId,
        Username = v.User.Username,
        DisplayName = v.User.DisplayName,
        Email = v.User.Email,
        AvatarUrl = v.User.ProfilePic,
        Status = v.Status.ToString(),
        HasDocument = !string.IsNullOrEmpty(v.DocumentStorageKey),
        DocumentUrl = !string.IsNullOrEmpty(v.DocumentStorageKey)
            ? $"/api/admin/verification/{v.Id}/document"
            : null,
        DocumentSubmittedAt = v.DocumentSubmittedAt?.ToUniversalTime().ToString("o"),
        AttemptCount = v.AttemptCount,
        ReviewedByUserId = v.ReviewedByUserId,
        ReviewedAt = v.ReviewedAt?.ToUniversalTime().ToString("o"),
        RejectionReason = v.RejectionReason,
        ConsentGivenAt = v.ConsentGivenAt?.ToUniversalTime().ToString("o"),
        CreatedAt = v.CreatedAt.ToUniversalTime().ToString("o"),
        DecisionLog = v.DecisionLog
    };

    private static string TruncateSummary(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "…";
}
