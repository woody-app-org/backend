using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Domain.Entities;

namespace Woody.Application.UseCases.Auth.Register;

public class RegisterHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDefaultCommunityBootstrap _defaultCommunity;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IDefaultCommunityBootstrap defaultCommunity,
        IJwtTokenService jwtTokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _defaultCommunity = defaultCommunity;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResultDTO> HandleAsync(RegisterRequestDTO request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.ExistsUsernameAsync(username))
            throw new InvalidOperationException("Nome de utilizador já existe.");
        if (await _users.ExistsEmailAsync(email))
            throw new InvalidOperationException("Email já registado.");

        if (!DateOnly.TryParse(request.BirthDate, out var birthDate))
            throw new ArgumentException("Data de nascimento inválida.");

        var user = new User
        {
            Username = username,
            Email = email,
            Password = _passwordHasher.HashPassword(request.Password),
            Role = "User",
            DisplayName = username,
            Cpf = request.Cpf.Trim(),
            BirthDate = birthDate,
            ProfilePic = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        await _defaultCommunity.EnsureUserInDefaultCommunityAsync(user.Id, cancellationToken);

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResultDTO
        {
            Token = token,
            User = new AuthUserDto
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                Name = user.DisplayName ?? user.Username,
                AvatarUrl = user.ProfilePic
            }
        };
    }
}
