namespace Woody.Application.DTOs;

public class ConfirmEmailVerificationCodeRequestDTO
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}
