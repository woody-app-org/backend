using System.ComponentModel.DataAnnotations;

namespace Woody.Application.DTOs.Admin;

public class AdminReportStatusUpdateRequest
{
    [Required]
    public string Status { get; set; } = null!;
    public string? InternalNote { get; set; }
    public string? ResolutionCode { get; set; }
}
