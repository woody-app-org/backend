namespace Woody.Application.DTOs;

public class CommunityUpdateRequestDTO
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<string>? Tags { get; set; }
    public string? Rules { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public string? Visibility { get; set; }
}
