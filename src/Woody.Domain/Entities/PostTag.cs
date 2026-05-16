namespace Woody.Domain.Entities;

public class PostTag
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public string Tag { get; set; } = null!;
}
