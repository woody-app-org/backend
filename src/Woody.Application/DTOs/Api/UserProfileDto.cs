using Woody.Application.DTOs;

namespace Woody.Application.DTOs.Api;

public class UserProfileDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Username { get; set; }

    /// <summary>
    /// Presente quando o handle pedido era um username antigo; indica o username actual para actualizar a URL.
    /// </summary>
    public string? CanonicalUsername { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Pronouns { get; set; }
    public string? BannerUrl { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string? Location { get; set; }
    /// <summary>Título ou profissão definidos pela utilizadora (não confundir com papel na plataforma).</summary>
    public string? Profession { get; set; }

    /// <summary>Presente apenas em <c>GET /users/me</c>. Não expor em perfis públicos.</summary>
    public string? VerificationStatus { get; set; }

    public List<SocialLinkDto> SocialLinks { get; set; } = new();
    public List<InterestItemResponseDto> Interests { get; set; } = new();
    public List<object> Suggestions { get; set; } = new();
    public bool? IsFollowing { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool ShowProBadge { get; set; }
    public bool HasActiveStories { get; set; }

    /// <summary>Insígnias conquistadas (ativas). Distinto de <see cref="ShowProBadge"/>.</summary>
    public List<UserBadgeDto> Badges { get; set; } = new();

    /// <summary>Presente apenas em <c>GET /users/me</c> (perfil da própria utilizadora).</summary>
    public UserSubscriptionStateDto? Subscription { get; set; }
}

public class InterestItemResponseDto
{
    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
}
