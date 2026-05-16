using Woody.Application.DTOs;
using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IAuthSessionService
{
    Task<LoginResultDTO> CreateSessionAsync(
        User user,
        UserSubscription? subscription,
        CancellationToken cancellationToken = default);

    Task<LoginResultDTO> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        string? refreshToken,
        int? expectedUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(int userId, string reason, CancellationToken cancellationToken = default);
}
