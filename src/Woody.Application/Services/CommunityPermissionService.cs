using Woody.Application.Interfaces;

namespace Woody.Application.Services;

/// <summary>
/// Permissões <strong>dentro de uma comunidade</strong> (membro ativo, owner, admin) e staff <c>Role=Admin</c>.
/// Isto é independente do plano Pro: criar comunidade exige Pro; moderar exige papel na comunidade (ou admin global).
/// </summary>
public class CommunityPermissionService : ICommunityPermissionService
{
    private readonly IUserRepository _users;
    private readonly ICommunityMembershipRepository _memberships;

    public CommunityPermissionService(IUserRepository users, ICommunityMembershipRepository memberships)
    {
        _users = users;
        _memberships = memberships;
    }

    public async Task<bool> CanPublishPostAsync(int communityId, int userId, CancellationToken cancellationToken = default)
    {
        var m = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(userId, communityId, cancellationToken);
        return m != null;
    }

    public async Task<bool> CanModerateCommunityAsync(int communityId, int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdNoTrackingAsync(userId, cancellationToken);
        if (user == null)
            return false;
        if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        var m = await _memberships.GetActiveForUserAndCommunityNoTrackingAsync(userId, communityId, cancellationToken);
        return m is { Role: "owner" or "admin" };
    }
}
