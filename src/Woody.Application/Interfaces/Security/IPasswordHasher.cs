namespace Woody.Application.Interfaces.Security
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
        PasswordVerificationOutcome VerifyPasswordWithOutcome(string hashedPassword, string providedPassword);
    }

    public sealed record PasswordVerificationOutcome(bool Succeeded, bool NeedsRehash);
}