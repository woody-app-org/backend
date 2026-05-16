namespace Woody.Application.DTOs;

public class UpdatePostRequestDTO
{
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public List<string>? Tags { get; set; }
}
