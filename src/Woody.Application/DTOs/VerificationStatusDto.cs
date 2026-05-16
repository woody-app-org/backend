namespace Woody.Application.DTOs;

/// <summary>
/// Resposta de status de verificação retornada para a própria usuária.
/// Nunca expõe DocumentStorageKey nem URL do documento.
/// </summary>
public class VerificationStatusDto
{
    public string Status { get; set; } = null!;
    public string? RejectionReason { get; set; }
    public string? DocumentSubmittedAt { get; set; }
    public string? ReviewedAt { get; set; }
    public int AttemptCount { get; set; }
}
