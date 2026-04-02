namespace Woody.Application.DTOs;

public class AuthUserDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
}
