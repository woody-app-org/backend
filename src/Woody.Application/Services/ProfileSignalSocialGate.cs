using Woody.Application.Interfaces;

namespace Woody.Application.Services;

/// <summary>
/// Ponte entre consumidores legados (<see cref="IProfileSignalSocialGate"/>) e o serviço central de bloqueio.
/// </summary>
public sealed class ProfileSignalSocialGate : IProfileSignalSocialGate
{
    private readonly IUserRelationshipVisibilityService _visibility;

    public ProfileSignalSocialGate(IUserRelationshipVisibilityService visibility)
    {
        _visibility = visibility;
    }

    public Task<bool> AreUsersBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default) =>
        _visibility.AreUsersBlockedEitherWayAsync(userIdA, userIdB, cancellationToken);
}
