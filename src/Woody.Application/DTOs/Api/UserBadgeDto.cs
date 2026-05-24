namespace Woody.Application.DTOs.Api;

/// <summary>Insígnia visível no perfil público — sem IDs internos.</summary>
public class UserBadgeDto
{
    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? IconAssetKey { get; set; }

    public string Category { get; set; } = null!;

    public string? Rarity { get; set; }

    public DateTime EarnedAt { get; set; }
}
