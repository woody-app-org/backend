namespace Woody.Application.DTOs.Api;

public class StoryDto
{
    public int Id { get; set; }
    public int AuthorUserId { get; set; }
    public UserPublicDto Author { get; set; } = null!;
    public string MediaType { get; set; } = null!;
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Text { get; set; }
    public string? BackgroundColor { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string ExpiresAt { get; set; } = null!;
    public int ViewCount { get; set; }
    public bool HasViewedByMe { get; set; }
}
