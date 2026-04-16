namespace Woody.Application.DTOs;

public class CreateCommunityRequestDTO
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
    public List<string>? Tags { get; set; }
    public string Rules { get; set; } = string.Empty;
    public string Visibility { get; set; } = "public";
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
}
