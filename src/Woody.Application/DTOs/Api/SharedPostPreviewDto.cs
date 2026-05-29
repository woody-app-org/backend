namespace Woody.Application.DTOs.Api;

/// <summary>Preview compacto de post partilhado numa mensagem directa.</summary>
public sealed class SharedPostPreviewDto
{
    public string? Id { get; set; }
    public string? PublicId { get; set; }
    public string? AuthorDisplayName { get; set; }
    public string? AuthorUsername { get; set; }
    public string? AuthorProfilePic { get; set; }
    public string? ContentPreview { get; set; }
    public string? FirstMediaUrl { get; set; }
    public string? FirstMediaType { get; set; }
    public string? CommunityName { get; set; }
    public bool IsUnavailable { get; set; }
}
