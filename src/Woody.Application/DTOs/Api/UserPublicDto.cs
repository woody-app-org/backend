namespace Woody.Application.DTOs.Api;

public class UserPublicDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
    public bool ShowProBadge { get; set; }
    public bool HasActiveStories { get; set; }
}
