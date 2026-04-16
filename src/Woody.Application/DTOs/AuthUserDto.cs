namespace Woody.Application.DTOs;

public class AuthUserDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public UserSubscriptionStateDto Subscription { get; set; } = null!;
}
