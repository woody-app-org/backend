namespace Woody.Application.DTOs;

public class MediaUploadResponseDto
{
    public string Url { get; set; } = null!;
    public string StorageKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
}
