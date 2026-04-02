namespace Woody.Application.DTOs.Api;

public class PostCommunityPreviewDto
{
    public string Id { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Category { get; set; } = null!;
}
