using Woody.Application.Utilities;

namespace Woody.Application.Tests;

public class EmailMaskingTests
{
    [Theory]
    [InlineData("nicholas@email.com", "ni*****@email.com")]
    [InlineData("a@email.com", "a***@email.com")]
    [InlineData("ab@domain.org", "ab*****@domain.org")]
    public void MaskForDisplay_FormatsAsExpected(string input, string expected) =>
        Assert.Equal(expected, EmailMasking.MaskForDisplay(input));

    [Fact]
    public void MaskForDisplay_InvalidInput_ReturnsSafePlaceholder() =>
        Assert.Equal("***@***", EmailMasking.MaskForDisplay("invalid"));
}
