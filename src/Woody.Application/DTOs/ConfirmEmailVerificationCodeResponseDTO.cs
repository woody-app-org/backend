namespace Woody.Application.DTOs;

public class ConfirmEmailVerificationCodeResponseDTO
{
    public bool Verified { get; set; }
    public DateTime VerifiedAt { get; set; }
}
