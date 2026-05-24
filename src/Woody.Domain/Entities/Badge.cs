namespace Woody.Domain.Entities;

/// <summary>Definição global de uma insígnia/conquista da plataforma (distinta de plano Pro/Max).</summary>
public class Badge
{
    public int Id { get; set; }

    /// <summary>Identificador estável (ex.: <c>seed</c>).</summary>
    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    /// <summary>Chave para o frontend resolver o asset local (ex.: <c>seed</c>).</summary>
    public string? IconAssetKey { get; set; }

    public string Category { get; set; } = null!;

    public string? Rarity { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
