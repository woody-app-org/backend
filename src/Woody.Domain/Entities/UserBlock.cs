namespace Woody.Domain.Entities;

public class UserBlock
{
    public int Id { get; set; }

    public int BlockerUserId { get; set; }
    public User BlockerUser { get; set; } = null!;

    public int BlockedUserId { get; set; }
    public User BlockedUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
