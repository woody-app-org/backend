using Woody.Application.DTOs;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.Validation;
using Woody.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Woody.Application.UseCases.Auth.Login;

public class LoginHandler
{
    public const string InvalidCredentialsMessage = "Credenciais inválidas.";

    private readonly IUserRepository _userRepository;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthSessionService _authSessions;
    private readonly ILoginLockoutRepository _lockouts;
    private readonly AuthSecurityOptions _authSecurityOptions;

    public LoginHandler(
        IUserRepository userRepository,
        IUserSubscriptionRepository subscriptions,
        IPasswordHasher passwordHasher,
        IAuthSessionService authSessions,
        ILoginLockoutRepository lockouts,
        IOptions<AuthSecurityOptions> authSecurityOptions)
    {
        _userRepository = userRepository;
        _subscriptions = subscriptions;
        _passwordHasher = passwordHasher;
        _authSessions = authSessions;
        _lockouts = lockouts;
        _authSecurityOptions = authSecurityOptions.Value;
    }

    public async Task<LoginResultDTO?> HandleAsync(LoginRequestDTO request, CancellationToken cancellationToken = default)
    {
        var login = !string.IsNullOrWhiteSpace(request.Username)
            ? request.Username.Trim()
            : request.Email?.Trim();

        if (string.IsNullOrEmpty(login))
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);

        var normalizedLogin = NormalizeLoginForLockout(login);
        var now = DateTime.UtcNow;
        var lockout = await _lockouts.GetByNormalizedLoginAsync(normalizedLogin, cancellationToken);
        if (lockout?.IsLockedAt(now) == true)
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);

        var user = await _userRepository.GetByUsernameOrEmailAsync(login);
        var password = PasswordInputValidator.NormalizeForLogin(request.Password);
        var verification = user == null
            ? new PasswordVerificationOutcome(false, false)
            : _passwordHasher.VerifyPasswordWithOutcome(user.Password, password);
        if (user == null || !verification.Succeeded)
        {
            await RecordFailedAttemptAsync(normalizedLogin, lockout, now, cancellationToken);
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        if (lockout != null)
        {
            _lockouts.Remove(lockout);
            await _lockouts.SaveChangesAsync(cancellationToken);
        }

        if (verification.NeedsRehash)
        {
            user.Password = _passwordHasher.HashPassword(password);
            user.UpdatedAt = now;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        var subscription = await _subscriptions.GetByUserIdNoTrackingAsync(user.Id, cancellationToken);
        return await _authSessions.CreateSessionAsync(user, subscription, cancellationToken);
    }

    private async Task RecordFailedAttemptAsync(
        string normalizedLogin,
        LoginLockout? existing,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var lockout = existing;
        if (lockout == null)
        {
            lockout = new LoginLockout
            {
                NormalizedLogin = normalizedLogin,
                FailedAttemptCount = 0,
                FirstFailedAt = utcNow
            };
            _lockouts.Add(lockout);
        }

        lockout.FailedAttemptCount += 1;
        lockout.LastFailedAt = utcNow;
        if (lockout.FailedAttemptCount >= _authSecurityOptions.MaxFailedLoginAttempts)
            lockout.LockoutEndAt = utcNow.AddMinutes(_authSecurityOptions.LockoutMinutes);

        await _lockouts.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeLoginForLockout(string login) =>
        login.Trim().ToLowerInvariant();
}
