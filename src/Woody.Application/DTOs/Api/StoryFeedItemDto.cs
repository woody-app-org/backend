namespace Woody.Application.DTOs.Api;

public class StoryFeedItemDto
{
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool HasActiveStories { get; set; } = true;
    public bool HasUnviewedStories { get; set; }
    public string? LastStoryCreatedAt { get; set; }
    public bool IsSelf { get; set; }
}
