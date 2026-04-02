namespace Woody.Domain.Entities;

public class Community
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    /// <summary>Valores alinhados ao front: bemestar, carreira, cultura, seguranca, outro</summary>
    public string Category { get; set; } = "outro";
    public string Rules { get; set; } = string.Empty;
    /// <summary>public | private</summary>
    public string Visibility { get; set; } = "public";
    public int OwnerUserId { get; set; }
    public User Owner { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CommunityTag> Tags { get; set; } = new List<CommunityTag>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<CommunityMembership> Memberships { get; set; } = new List<CommunityMembership>();
    public ICollection<JoinRequest> JoinRequests { get; set; } = new List<JoinRequest>();
}
