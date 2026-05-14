using Woody.Application.Validation;

namespace Woody.Application.Tests;

public sealed class PostHashtagNormalizationTests
{
    [Fact]
    public void TryNormalizePostHashtags_strips_leading_hash_and_dedupes()
    {
        var ok = InputValidator.TryNormalizePostHashtags(
            new[] { "#tech", " tech ", "#TECH", "carreira" },
            out var list,
            out var err);

        Assert.True(ok, err);
        Assert.Null(err);
        Assert.Equal(new[] { "tech", "carreira" }, list);
    }

    [Fact]
    public void TryNormalizePostHashtags_rejects_more_than_three()
    {
        var ok = InputValidator.TryNormalizePostHashtags(
            new[] { "a", "b", "c", "d" },
            out _,
            out var err);

        Assert.False(ok);
        Assert.Contains("3", err, StringComparison.Ordinal);
    }

    [Fact]
    public void TryNormalizePostHashtags_rejects_inner_hash()
    {
        var ok = InputValidator.TryNormalizePostHashtags(
            new[] { "bad#tag" },
            out _,
            out var err);

        Assert.False(ok);
        Assert.NotNull(err);
    }

    [Fact]
    public void TryNormalizePostHashtags_rejects_too_long_token()
    {
        var ok = InputValidator.TryNormalizePostHashtags(
            new[] { new string('x', InputValidationLimits.PostHashtagMaxLength + 1) },
            out _,
            out var err);

        Assert.False(ok);
        Assert.NotNull(err);
    }
}
