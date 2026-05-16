namespace Woody.Application.DTOs.Admin;

/// <summary>
/// Item retornado na listagem do dashboard de verificação.
/// Nunca expõe DocumentStorageKey nem URL do documento.
/// </summary>
public class AdminVerificationListItemDto
{
    public int VerificationId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = null!;
    public string? DocumentSubmittedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? ReviewedAt { get; set; }
    public string? RejectionReasonSummary { get; set; }
}
