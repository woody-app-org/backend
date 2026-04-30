namespace Woody.Application.DTOs.Api;

public sealed class MessageAttachmentItemRequestDto
{
    public string Url { get; set; } = null!;
    /// <summary><c>image</c> | <c>video</c> | <c>gif</c> | <c>sticker</c></summary>
    public string MediaType { get; set; } = null!;
    public int? DurationSeconds { get; set; }
    public string? Provider { get; set; }
    public string? ExternalId { get; set; }
}
