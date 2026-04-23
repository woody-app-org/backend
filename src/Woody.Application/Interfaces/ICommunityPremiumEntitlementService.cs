using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

/// <summary>Resolve capacidades premium da comunidade (papel + plano) para a utilizadora autenticada.</summary>
public interface ICommunityPremiumEntitlementService
{
    Task<CommunityPremiumCapabilitiesDto> GetCapabilitiesAsync(int communityId, int userId,
        CancellationToken cancellationToken = default);
}
