namespace Woody.Domain.Entities;

public class JoinRequest
{
    public int Id { get; set; }
    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    /// <summary>pending | approved | rejected | cancelled</summary>
    public string Status { get; set; } = "pending";
    public DateTime RequestedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    /// <summary>Moderadora que aprovou/recusou (não preenchido em cancelamento pela própria utilizadora).</summary>
    public User? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
