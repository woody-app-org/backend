namespace Woody.Application.DTOs;

public class VerifyPasswordResetCodeResponseDTO
{
    public string ResetToken { get; set; } = null!;
    public int ExpiresInSeconds { get; set; }
}
