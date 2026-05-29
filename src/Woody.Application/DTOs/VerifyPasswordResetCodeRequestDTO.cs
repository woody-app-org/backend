namespace Woody.Application.DTOs;

public class VerifyPasswordResetCodeRequestDTO
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}
