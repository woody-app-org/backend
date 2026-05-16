namespace Woody.Domain.Entities;

public class RefreshTokenSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevocationReason { get; set; }

    public bool IsActiveAt(DateTime utcNow) => RevokedAt == null && ExpiresAt > utcNow;
}
