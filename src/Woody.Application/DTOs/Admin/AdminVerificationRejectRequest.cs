using System.ComponentModel.DataAnnotations;

namespace Woody.Application.DTOs.Admin;

public class AdminVerificationRejectRequest
{
    [Required]
    [MinLength(10, ErrorMessage = "O motivo de recusa deve ter ao menos 10 caracteres.")]
    [MaxLength(500, ErrorMessage = "O motivo de recusa deve ter no máximo 500 caracteres.")]
    public string RejectionReason { get; set; } = null!;
}
