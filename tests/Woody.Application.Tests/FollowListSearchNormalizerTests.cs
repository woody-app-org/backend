using Woody.Application.Validation;

namespace Woody.Application.Tests;

public class FollowListSearchNormalizerTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("@", null)]
    [InlineData(" ana ", "ana")]
    [InlineData("@ana", "ana")]
    [InlineData("@Ana_Souza", "ana_souza")]
    [InlineData("  @  maria  ", "maria")]
    public void Normalize_TrimsAtPrefixAndLowercases(string? raw, string? expected)
    {
        Assert.Equal(expected, FollowListSearchNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_TruncatesToMaxLength()
    {
        var raw = new string('A', InputValidationLimits.FollowListSearchMaxLength + 25);
        var normalized = FollowListSearchNormalizer.Normalize(raw);

        Assert.NotNull(normalized);
        Assert.Equal(InputValidationLimits.FollowListSearchMaxLength, normalized!.Length);
        Assert.Equal(new string('a', InputValidationLimits.FollowListSearchMaxLength), normalized);
    }
}
