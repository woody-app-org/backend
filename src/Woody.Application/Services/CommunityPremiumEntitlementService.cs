using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Subscription;

namespace Woody.Application.Services;

/// <summary>
/// Agrega papel na comunidade + estado de <c>CommunitySubscription</c>. Não acede a <c>UserSubscription</c>.
/// </summary>
public class CommunityPremiumEntitlementService : ICommunityPremiumEntitlementService
{
    private readonly ICommunityRepository _communities;
    private readonly ICommunityMembershipRepository _memberships;

    public CommunityPremiumEntitlementService(
        ICommunityRepository communities,
        ICommunityMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task<CommunityPremiumCapabilitiesDto> GetCapabilitiesAsync(int communityId, int userId,
        CancellationToken cancellationToken = default)
    {
        var membership =
            await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(userId, communityId, cancellationToken);
        var community = await _communities.GetByIdWithTagsNoTrackingAsync(communityId, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var staff = CommunityPremiumFeatureGate.IsStaffForPremiumTools(membership?.Role);
        var premium = CommunityPremiumFeatureGate.CommunityPremiumIsActive(community?.Subscription, utcNow);

        return new CommunityPremiumCapabilitiesDto
        {
            IsStaffForPremiumTools = staff,
            CommunityPremiumActive = premium,
            CanAccessCommunityAnalytics =
                CommunityPremiumFeatureGate.CanAccessCommunityAnalytics(membership, community?.Subscription, utcNow),
            CanBoostCommunityPosts =
                CommunityPremiumFeatureGate.CanBoostCommunityPost(membership, community?.Subscription, utcNow)
        };
    }
}
