namespace Woody.Domain.Entities;

public class ContentReport
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public User Reporter { get; set; } = null!;
    /// <summary>post | comment</summary>
    public string TargetType { get; set; } = null!;
    public int? PostId { get; set; }
    public Post? Post { get; set; }
    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }
    public string ReasonCode { get; set; } = null!;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
