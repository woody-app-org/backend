namespace Woody.Application.DTOs.Api;

public class CommunityResponseDto
{
    public string Id { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public string Rules { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public string OwnerUserId { get; set; } = null!;
    public string Visibility { get; set; } = null!;
    public int MemberCount { get; set; }
}
