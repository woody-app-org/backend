namespace Woody.Domain.Entities;

public class JoinRequest
{
    public int Id { get; set; }
    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    /// <summary>pending | approved | rejected</summary>
    public string Status { get; set; } = "pending";
    public DateTime RequestedAt { get; set; }
}
