namespace Woody.Domain.Entities;

public class UserSocialLink
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Handle { get; set; }
}
