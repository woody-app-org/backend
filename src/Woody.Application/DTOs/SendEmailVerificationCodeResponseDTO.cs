namespace Woody.Application.DTOs;

public class SendEmailVerificationCodeResponseDTO
{
    public string RequestId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
