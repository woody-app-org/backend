using Woody.Application.Validation;

namespace Woody.Application.Tests;

public class PasswordInputValidatorTests
{
    [Theory]
    [InlineData("secret", "secret")]
    [InlineData(" sec ret ", "secret")]
    [InlineData("\tpass\nword\t", "password")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeForLogin_RemovesAllWhitespace(string? raw, string expected)
    {
        Assert.Equal(expected, PasswordInputValidator.NormalizeForLogin(raw));
    }

    [Fact]
    public void TryValidateForRegistration_RejectsWhitespace()
    {
        var ok = PasswordInputValidator.TryValidateForRegistration(
            "senha 1234",
            maxLength: 256,
            minLength: 8,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal(PasswordInputValidator.ContainsWhitespaceMessage, error);
    }

    [Fact]
    public void TryValidateForRegistration_AcceptsPasswordWithoutSpaces()
    {
        var ok = PasswordInputValidator.TryValidateForRegistration(
            "senha1234",
            maxLength: 256,
            minLength: 8,
            out var normalized,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("senha1234", normalized);
    }

    [Fact]
    public void TryValidateForRegistration_RejectsLeadingTrailingSpaces()
    {
        var ok = PasswordInputValidator.TryValidateForRegistration(
            " senha1234",
            maxLength: 256,
            minLength: 8,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal(PasswordInputValidator.ContainsWhitespaceMessage, error);
    }
}
