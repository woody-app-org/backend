namespace Woody.Application.DTOs.Admin;

/// <summary>
/// Detalhe completo de uma solicitação de verificação para o dashboard admin.
/// Inclui documentUrl que aponta para o endpoint proxy protegido.
/// Nunca expõe DocumentStorageKey.
/// </summary>
public class AdminVerificationDetailDto
{
    public int VerificationId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = null!;
    public bool HasDocument { get; set; }
    /// <summary>
    /// URL interna do endpoint proxy de documento. Null se não houver documento.
    /// Requer autenticação SuperAdmin para acessar.
    /// </summary>
    public string? DocumentUrl { get; set; }
    public string? DocumentSubmittedAt { get; set; }
    public int AttemptCount { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ConsentGivenAt { get; set; }
    public string? CreatedAt { get; set; }
    /// <summary>Log de decisões em JSON. Não contém storageKey.</summary>
    public string? DecisionLog { get; set; }
}
