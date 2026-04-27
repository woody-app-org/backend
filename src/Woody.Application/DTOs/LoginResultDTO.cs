namespace Woody.Application.DTOs;

public class LoginResultDTO
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public AuthUserDto User { get; set; } = null!;
}
