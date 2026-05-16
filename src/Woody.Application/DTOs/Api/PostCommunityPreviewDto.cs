namespace Woody.Application.DTOs.Api;

public class PostCommunityPreviewDto
{
    public string Id { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Category { get; set; } = null!;

    /// <summary>Plano efetivo da comunidade (<c>free</c> | <c>premium</c>) para gates no feed sem chamada extra.</summary>
    public string CommunityPlan { get; set; } = "free";
}
