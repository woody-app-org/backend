namespace Woody.Domain.Entities;

public class BetaInvite
{
    public int Id { get; set; }

    /// <summary>Código normalizado (maiúsculas, sem espaços à volta).</summary>
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public int MaxUses { get; set; }

    public int UsesCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Opcional: utilizadora ou sistema que criou o convite.</summary>
    public string? CreatedBy { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
