using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Domain.Entities;

namespace Woody.Application.UseCases.Auth.Login;

public class LoginHandler
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public LoginHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResultDTO?> HandleAsync(LoginRequestDTO request)
    {
        var login = !string.IsNullOrWhiteSpace(request.Username)
            ? request.Username.Trim()
            : request.Email?.Trim();

        if (string.IsNullOrEmpty(login))
            throw new UnauthorizedAccessException();

        var user = await _userRepository.GetByUsernameOrEmailAsync(login);
        if (user == null || !_passwordHasher.VerifyPassword(user.Password, request.Password))
            throw new UnauthorizedAccessException();

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResultDTO
        {
            Token = token,
            User = MapUser(user)
        };
    }

    private static AuthUserDto MapUser(User user) => new()
    {
        Id = user.Id.ToString(),
        Username = user.Username,
        Email = user.Email,
        IsEmailVerified = user.IsEmailVerified,
        Name = user.DisplayName ?? user.Username,
        AvatarUrl = user.ProfilePic
    };
}
