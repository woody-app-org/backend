namespace Woody.Domain.Entities;

public class PreLaunchSignup
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string SocialNetwork { get; set; } = null!;

    public string SocialUsername { get; set; } = null!;

    /// <summary>Rede social em minúsculas (instagram, tiktok, x, threads, linkedin, other).</summary>
    public string NormalizedSocialNetwork { get; set; } = null!;

    /// <summary>Username sem @ e em minúsculas para deduplicação.</summary>
    public string NormalizedSocialUsername { get; set; } = null!;

    /// <summary>SHA256(IP + segredo), nunca IP em claro.</summary>
    public string? IpHash { get; set; }

    /// <summary>SHA256(User-Agent + segredo).</summary>
    public string? UserAgentHash { get; set; }

    /// <summary>Momento em que o usuário aceitou receber contato.</summary>
    public DateTime AcceptedContactAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Canal de origem opcional (ex.: "landing", "qr-evento").</summary>
    public string? Source { get; set; }
}
