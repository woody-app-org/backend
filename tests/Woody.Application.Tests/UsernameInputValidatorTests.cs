using Woody.Application.Validation;

namespace Woody.Application.Tests;

public class UsernameInputValidatorTests
{
    [Theory]
    [InlineData("Nicholas_Navarro", "nicholas_navarro")]
    [InlineData(" NICHOLAS_NAVARRO ", "nicholas_navarro")]
    [InlineData("user.name_1", "user.name_1")]
    public void Normalize_TrimsAndLowercases(string raw, string expected) =>
        Assert.Equal(expected, UsernameInputValidator.Normalize(raw));

    [Fact]
    public void TryValidate_AcceptsValidUsername()
    {
        Assert.True(UsernameInputValidator.TryValidate("Maria_Silva", out var normalized, out var error));
        Assert.Equal("maria_silva", normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("  ")]
    public void TryValidate_RejectsTooShort(string raw)
    {
        Assert.False(UsernameInputValidator.TryValidate(raw, out _, out var error));
        Assert.Equal(UsernameInputValidator.TooShortMessage, error);
    }

    [Fact]
    public void TryValidate_RejectsOverMaxLength()
    {
        var raw = new string('a', InputValidationLimits.UsernameMaxLength + 1);
        Assert.False(UsernameInputValidator.TryValidate(raw, out _, out var error));
        Assert.Contains("30", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("josé")]
    [InlineData("user/name")]
    [InlineData("user?")]
    [InlineData("user#tag")]
    [InlineData("user@mail")]
    [InlineData("user%")]
    [InlineData("user-name")]
    public void TryValidate_RejectsInvalidCharacters(string raw)
    {
        Assert.False(UsernameInputValidator.TryValidate(raw, out _, out var error));
        Assert.Equal(UsernameInputValidator.InvalidCharactersMessage, error);
    }

    [Fact]
    public void TryValidate_CaseVariantsNormalizeToSameValue()
    {
        Assert.True(UsernameInputValidator.TryValidate("Nicholas", out var first, out _));
        Assert.True(UsernameInputValidator.TryValidate("nicholas", out var second, out _));
        Assert.Equal(first, second);
    }
}
