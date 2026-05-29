namespace Woody.Application.DTOs;

public class ConfirmPasswordResetRequestDTO
{
    public string ResetToken { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
