using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.Mapping;
using Woody.Domain.Entities;

namespace Woody.Application.UseCases.Auth.Login;

public class LoginHandler
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IPasswordHasher _passwordHasher;

    public LoginHandler(
        IUserRepository userRepository,
        IUserSubscriptionRepository subscriptions,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _subscriptions = subscriptions;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResultDTO?> HandleAsync(LoginRequestDTO request, CancellationToken cancellationToken = default)
    {
        var login = !string.IsNullOrWhiteSpace(request.Username)
            ? request.Username.Trim()
            : request.Email?.Trim();

        if (string.IsNullOrEmpty(login))
            throw new UnauthorizedAccessException();

        var user = await _userRepository.GetByUsernameOrEmailAsync(login);
        if (user == null || !_passwordHasher.VerifyPassword(user.Password, request.Password))
            throw new UnauthorizedAccessException();

        var subscription = await _subscriptions.GetByUserIdNoTrackingAsync(user.Id, cancellationToken);
        var token = _jwtTokenService.GenerateToken(user, subscription);
        var utcNow = DateTime.UtcNow;

        return new LoginResultDTO
        {
            Token = token,
            User = AuthUserMapper.From(user, subscription, utcNow)
        };
    }
}
