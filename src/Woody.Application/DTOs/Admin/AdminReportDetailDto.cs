using Woody.Application.DTOs.Api;

namespace Woody.Application.DTOs.Admin;

public class AdminReportDetailDto
{
    public int Id { get; set; }
    public string TargetType { get; set; } = null!;
    public string ReasonCode { get; set; } = null!;
    public string? Details { get; set; }
    public string Status { get; set; } = null!;
    public string? InternalNote { get; set; }
    public string? ResolutionCode { get; set; }

    public UserPublicDto ReporterUser { get; set; } = null!;
    public UserPublicDto? ReportedContentAuthor { get; set; }
    public AdminReportReviewerDto? ReviewedBy { get; set; }

    public AdminReportPostDetailDto? Post { get; set; }
    public AdminReportCommentDetailDto? Comment { get; set; }

    public int SameTargetReportCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class AdminReportReviewerDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
}

public class AdminReportPostDetailDto
{
    public int Id { get; set; }
    public string PublicId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminReportMediaItemDto> Media { get; set; } = [];
}

public class AdminReportCommentDetailDto
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Post ao qual o comentário pertence.</summary>
    public AdminReportPostDetailDto? ParentPost { get; set; }
}

public class AdminReportMediaItemDto
{
    public string Kind { get; set; } = null!;
    public string Url { get; set; } = null!;
}
