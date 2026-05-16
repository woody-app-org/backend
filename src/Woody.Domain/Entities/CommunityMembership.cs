namespace Woody.Domain.Entities;

public class CommunityMembership
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;
    /// <summary>owner | admin | member</summary>
    public string Role { get; set; } = "member";
    /// <summary>active | pending | rejected | banned</summary>
    public string Status { get; set; } = "active";
    public DateTime? JoinedAt { get; set; }
}
