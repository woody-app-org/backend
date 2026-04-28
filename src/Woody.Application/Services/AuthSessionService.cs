using System.Security.Cryptography;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.Mapping;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public class AuthSessionService : IAuthSessionService
{
    private const int RefreshTokenByteLength = 64;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IRefreshTokenSessionRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthSessionService(
        IRefreshTokenSessionRepository refreshTokens,
        IUserRepository users,
        IUserSubscriptionRepository subscriptions,
        IJwtTokenService jwtTokenService)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _subscriptions = subscriptions;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResultDTO> CreateSessionAsync(
        User user,
        UserSubscription? subscription,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var refreshToken = GenerateRefreshToken();
        await _refreshTokens.AddAsync(
            new RefreshTokenSession
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(refreshToken),
                CreatedAt = now,
                ExpiresAt = now.Add(RefreshTokenLifetime)
            },
            cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return BuildLoginResult(user, subscription, refreshToken, now);
    }

    public async Task<LoginResultDTO> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var now = DateTime.UtcNow;
        var currentHash = HashRefreshToken(refreshToken.Trim());
        var current = await _refreshTokens.GetByTokenHashAsync(currentHash, cancellationToken);
        if (current == null || !current.IsActiveAt(now))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var user = await _users.GetByIdTrackedAsync(current.UserId, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Credenciais inválidas.");
        var subscription = await _subscriptions.GetByUserIdNoTrackingAsync(user.Id, cancellationToken);

        var nextRefreshToken = GenerateRefreshToken();
        var nextHash = HashRefreshToken(nextRefreshToken);
        current.RevokedAt = now;
        current.ReplacedByTokenHash = nextHash;
        current.RevocationReason = "rotated";

        await _refreshTokens.AddAsync(
            new RefreshTokenSession
            {
                UserId = user.Id,
                TokenHash = nextHash,
                CreatedAt = now,
                ExpiresAt = now.Add(RefreshTokenLifetime)
            },
            cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return BuildLoginResult(user, subscription, nextRefreshToken, now);
    }

    public async Task RevokeRefreshTokenAsync(
        string? refreshToken,
        int? expectedUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var session = await _refreshTokens.GetByTokenHashAsync(HashRefreshToken(refreshToken.Trim()), cancellationToken);
        if (session == null || session.RevokedAt.HasValue)
            return;

        if (expectedUserId.HasValue && session.UserId != expectedUserId.Value)
            return;

        session.RevokedAt = DateTime.UtcNow;
        session.RevocationReason = reason;
        await _refreshTokens.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, string reason, CancellationToken cancellationToken = default)
    {
        await _refreshTokens.RevokeActiveForUserAsync(userId, DateTime.UtcNow, reason, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);
    }

    public static string HashRefreshToken(string refreshToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private LoginResultDTO BuildLoginResult(
        User user,
        UserSubscription? subscription,
        string refreshToken,
        DateTime utcNow) =>
        new()
        {
            Token = _jwtTokenService.GenerateToken(user, subscription),
            RefreshToken = refreshToken,
            User = AuthUserMapper.From(user, subscription, utcNow)
        };

    private static string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[RefreshTokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
