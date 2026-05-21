namespace Woody.Application.DTOs.Api;

/// <summary>Resumo mínimo de uma comunidade em que a utilizadora é membro activa (<c>GET /users/me/communities</c>).</summary>
public class MyCommunitySummaryDto
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Visibility { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
}
