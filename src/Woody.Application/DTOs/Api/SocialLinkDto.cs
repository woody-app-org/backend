namespace Woody.Application.DTOs.Api;

public class SocialLinkDto
{
    public string Id { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Handle { get; set; }
}
