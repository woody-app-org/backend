using Woody.Infrastructure.Security;

namespace Woody.Infrastructure.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_AllowsRoundTripVerification()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.HashPassword("correct horse battery staple");

        var outcome = hasher.VerifyPasswordWithOutcome(hash, "correct horse battery staple");

        Assert.True(outcome.Succeeded);
        Assert.False(outcome.NeedsRehash);
        Assert.NotEqual("correct horse battery staple", hash);
    }

    [Fact]
    public void VerifyPassword_RejectsWrongPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("correct horse battery staple");

        var outcome = hasher.VerifyPasswordWithOutcome(hash, "wrong password");

        Assert.False(outcome.Succeeded);
        Assert.False(outcome.NeedsRehash);
    }
}
