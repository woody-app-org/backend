namespace Woody.Domain.Entities;

public class PasswordResetSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActiveAt(DateTime utcNow) => ConsumedAt == null && ExpiresAt > utcNow;
}
