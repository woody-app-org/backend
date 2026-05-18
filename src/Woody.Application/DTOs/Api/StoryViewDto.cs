namespace Woody.Application.DTOs.Api;

public class StoryViewDto
{
    public int ViewerUserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string ViewedAt { get; set; } = null!;
}
