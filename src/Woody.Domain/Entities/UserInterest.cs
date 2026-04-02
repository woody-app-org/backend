namespace Woody.Domain.Entities;

public class UserInterest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Label { get; set; } = null!;
}
