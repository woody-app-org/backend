namespace Woody.Application.DTOs.Api;

/// <summary>GIF opcional anexado a um comentário (no máximo um).</summary>
public sealed class CommentGifResponseDto
{
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public string Provider { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
    public string? Title { get; set; }
}
