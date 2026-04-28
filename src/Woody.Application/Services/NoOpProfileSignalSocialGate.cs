using Woody.Application.Interfaces;

namespace Woody.Application.Services;

/// <summary>
/// Placeholder até existir persistência de bloqueios; substituir por implementação EF sem alterar <see cref="ProfileSignalService"/>.
/// </summary>
public sealed class NoOpProfileSignalSocialGate : IProfileSignalSocialGate
{
    public Task<bool> AreUsersBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
