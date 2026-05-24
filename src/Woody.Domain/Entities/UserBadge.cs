namespace Woody.Domain.Entities;

/// <summary>Insígnia conquistada por uma utilizadora.</summary>
public class UserBadge
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BadgeId { get; set; }

    public DateTime EarnedAt { get; set; }

    /// <summary>Metadados opcionais (JSON). Não exposto em DTOs públicos.</summary>
    public string? MetadataJson { get; set; }

    public User User { get; set; } = null!;

    public Badge Badge { get; set; } = null!;
}
