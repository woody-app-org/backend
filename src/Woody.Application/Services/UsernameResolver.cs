using Woody.Application.Interfaces;
using Woody.Application.Validation;

namespace Woody.Application.Services;

/// <summary>Resolve handle de URL (username actual ou histórico) para utilizador.</summary>
public sealed class UsernameResolver
{
    private readonly IUserRepository _users;
    private readonly IUsernameHistoryRepository _usernameHistory;

    public UsernameResolver(IUserRepository users, IUsernameHistoryRepository usernameHistory)
    {
        _users = users;
        _usernameHistory = usernameHistory;
    }

    public readonly record struct UsernameResolution(int UserId, string CurrentUsername, bool ResolvedViaHistory);

    public async Task<UsernameResolution?> ResolveAsync(string rawHandle, CancellationToken cancellationToken = default)
    {
        var normalized = UsernameInputValidator.Normalize(rawHandle);
        if (string.IsNullOrEmpty(normalized))
            return null;

        var user = await _users.GetByUsernameAsync(normalized);
        if (user != null)
            return new UsernameResolution(user.Id, user.Username, ResolvedViaHistory: false);

        var userId = await _usernameHistory.GetUserIdByOldUsernameAsync(normalized, cancellationToken);
        if (userId == null)
            return null;

        var viaHistory = await _users.GetByIdNoTrackingAsync(userId.Value, cancellationToken);
        if (viaHistory == null)
            return null;

        return new UsernameResolution(viaHistory.Id, viaHistory.Username, ResolvedViaHistory: true);
    }
}
