using IdentityPasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;
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
            return VerifyPasswordWithOutcome(hashedPassword, providedPassword).Succeeded;
        }

        public PasswordVerificationOutcome VerifyPasswordWithOutcome(string hashedPassword, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
            return result switch
            {
                IdentityPasswordVerificationResult.Success => new PasswordVerificationOutcome(true, false),
                IdentityPasswordVerificationResult.SuccessRehashNeeded => new PasswordVerificationOutcome(true, true),
                _ => new PasswordVerificationOutcome(false, false)
            };
        }
    }
}