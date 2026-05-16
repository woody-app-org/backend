namespace Woody.Domain.Entities;

public class CommunityTag
{
    public int Id { get; set; }
    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;
    public string Tag { get; set; } = null!;
}
