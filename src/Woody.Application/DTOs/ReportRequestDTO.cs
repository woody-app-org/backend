namespace Woody.Application.DTOs;

public class ReportRequestDTO
{
    public string TargetType { get; set; } = null!;
    public string? PostId { get; set; }
    public string? CommentId { get; set; }
    public string ReasonCode { get; set; } = null!;
    public string? Details { get; set; }
}
