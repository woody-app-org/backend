using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

/// <summary>
/// Fonte de verdade para <b>ferramentas premium ao nível do espaço</b> (analytics, impulsionamento, checkout premium da comunidade).
/// Combina apenas: (1) papel na comunidade (owner/admin) e (2) estado da assinatura <i>da comunidade</i>.
/// Não consulta nem devolve o plano Woody Pro da utilizadora — esse eixo vive em <c>IUserEntitlementService</c> / assinatura de utilizadora.
/// </summary>
public interface ICommunityPremiumEntitlementService
{
    Task<CommunityPremiumCapabilitiesDto> GetCapabilitiesAsync(int communityId, int userId,
        CancellationToken cancellationToken = default);
}
