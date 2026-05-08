using Woody.Application.DTOs;

namespace Woody.Application.DTOs.Api;

public class UserProfileDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Pronouns { get; set; }
    public string? BannerUrl { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Role { get; set; }

    /// <summary>Presente apenas em <c>GET /users/me</c>. Não expor em perfis públicos.</summary>
    public string? VerificationStatus { get; set; }

    public List<SocialLinkDto> SocialLinks { get; set; } = new();
    public List<InterestItemResponseDto> Interests { get; set; } = new();
    public List<object> Suggestions { get; set; } = new();
    public bool? IsFollowing { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool ShowProBadge { get; set; }

    /// <summary>Presente apenas em <c>GET /users/me</c> (perfil da própria utilizadora).</summary>
    public UserSubscriptionStateDto? Subscription { get; set; }
}

public class InterestItemResponseDto
{
    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
}
