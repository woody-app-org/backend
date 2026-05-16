namespace Woody.Domain.Entities;

public class LoginLockout
{
    public string NormalizedLogin { get; set; } = null!;
    public int FailedAttemptCount { get; set; }
    public DateTime FirstFailedAt { get; set; }
    public DateTime LastFailedAt { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    public bool IsLockedAt(DateTime utcNow) => LockoutEndAt.HasValue && LockoutEndAt > utcNow;
}
