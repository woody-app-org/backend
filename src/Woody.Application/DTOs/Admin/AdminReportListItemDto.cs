using Woody.Application.DTOs.Api;

namespace Woody.Application.DTOs.Admin;

public class AdminReportListItemDto
{
    public int Id { get; set; }
    public string TargetType { get; set; } = null!;
    public string ReasonCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public UserPublicDto ReporterUser { get; set; } = null!;
    public UserPublicDto? ReportedContentAuthor { get; set; }
    public AdminReportTargetPreviewDto TargetPreview { get; set; } = null!;
    public int SameTargetReportCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminReportTargetPreviewDto
{
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
    /// <summary>Trecho inicial do conteúdo denunciado (máx. 280 chars).</summary>
    public string? ContentSnippet { get; set; }
}
