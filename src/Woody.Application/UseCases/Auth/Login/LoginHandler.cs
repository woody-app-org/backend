using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Domain.Entities;

namespace Woody.Application.UseCases.Auth.Login
{
    public class LoginHandler
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        public LoginHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResultDTO?> HandleAsync(LoginRequestDTO request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordHasher.VerifyPassword(user.Password, request.Password))
                throw new UnauthorizedAccessException();

            var token = _jwtTokenService.GenerateToken(user);

            return new LoginResultDTO { Token = token };
        }
    }
}