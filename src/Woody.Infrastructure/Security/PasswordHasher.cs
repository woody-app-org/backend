using Microsoft.AspNetCore.Identity;
using Woody.Application.Interfaces.Security;
using Woody.Domain.Entities;

namespace Woody.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<User> _hasher = new();
        public string HashPassword(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}