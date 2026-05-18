namespace Woody.Application.DTOs.Api;

public class CreateStoryRequestDto
{
    public string MediaType { get; set; } = null!;
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? StorageKey { get; set; }
    public string? Text { get; set; }
    public string? BackgroundColor { get; set; }
}
