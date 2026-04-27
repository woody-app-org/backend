namespace Woody.Application.DTOs;

public class InterestItemDto
{
    public string? Id { get; set; }
    public string Label { get; set; } = null!;
}

public class UpdateProfileRequestDTO
{
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Bio { get; set; } = string.Empty;
    public string? Pronouns { get; set; }
    public string? Location { get; set; }
    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public List<InterestItemDto>? Interests { get; set; }
}
