namespace Woody.Application.DTOs.Api;

/// <summary>
/// Capacidades premium ao nível da comunidade para a utilizadora atual.
/// <see cref="IsStaffForPremiumTools"/> e <see cref="CommunityPremiumActive"/> são independentes; os <c>Can*</c> combinam ambos.
/// </summary>
public sealed class CommunityPremiumCapabilitiesDto
{
    public bool IsStaffForPremiumTools { get; set; }
    public bool CommunityPremiumActive { get; set; }
    public bool CanAccessCommunityAnalytics { get; set; }
    public bool CanBoostCommunityPosts { get; set; }
}
