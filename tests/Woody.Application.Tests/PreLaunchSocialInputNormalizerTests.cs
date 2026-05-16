using Woody.Application.PreLaunch;

namespace Woody.Application.Tests;

public class PreLaunchSocialInputNormalizerTests
{
    [Theory]
    [InlineData("instagram", "@Maria", "Maria", "maria")]
    [InlineData("instagram", " MariaSilva ", "MariaSilva", "mariasilva")]
    [InlineData("instagram", "https://instagram.com/maria.silva", "maria.silva", "maria.silva")]
    [InlineData("instagram", "instagram.com/maria.silva/", "maria.silva", "maria.silva")]
    public void NormalizeUsername_InstagramExamples(
        string network,
        string input,
        string expectedDisplay,
        string expectedNorm)
    {
        var net = PreLaunchSocialInputNormalizer.TryNormalizeNetwork(network);
        Assert.Equal("instagram", net);

        var r = PreLaunchSocialInputNormalizer.NormalizeUsername(net!, input);
        Assert.True(r.Success);
        Assert.Equal(expectedDisplay, r.DisplayUsername);
        Assert.Equal(expectedNorm, r.NormalizedUsername);
    }

    [Fact]
    public void NormalizeUsername_SecondInstagramSubmitSameAsFirst_IsSameNormalizedKey()
    {
        var a = PreLaunchSocialInputNormalizer.NormalizeUsername("instagram", "@Maria");
        var b = PreLaunchSocialInputNormalizer.NormalizeUsername("instagram", "maria");
        Assert.True(a.Success);
        Assert.True(b.Success);
        Assert.Equal(a.NormalizedUsername, b.NormalizedUsername);
    }

    [Fact]
    public void NormalizeUsername_SameHandleDifferentNetworks_DifferentRowsInDb()
    {
        var ig = PreLaunchSocialInputNormalizer.NormalizeUsername("instagram", "maria");
        var fb = PreLaunchSocialInputNormalizer.NormalizeUsername("facebook", "maria");
        Assert.True(ig.Success);
        Assert.True(fb.Success);
        Assert.Equal("maria", ig.NormalizedUsername);
        Assert.Equal("maria", fb.NormalizedUsername);
    }

    [Fact]
    public void TryNormalizeNetwork_AcceptsLinkedInAndOther_RejectsUnknown()
    {
        Assert.Equal("linkedin", PreLaunchSocialInputNormalizer.TryNormalizeNetwork("LinkedIn"));
        Assert.Equal("other", PreLaunchSocialInputNormalizer.TryNormalizeNetwork("OTHER"));
        Assert.Equal("facebook", PreLaunchSocialInputNormalizer.TryNormalizeNetwork("Facebook"));
    }

    [Fact]
    public void NormalizeUsername_Other_AllowsInternalSpace()
    {
        var r = PreLaunchSocialInputNormalizer.NormalizeUsername("other", "  Minha rede  ");
        Assert.True(r.Success);
        Assert.Equal("Minha rede", r.DisplayUsername);
        Assert.Equal("minha rede", r.NormalizedUsername);
    }

    [Fact]
    public void SanitizeName_RemovesControlCharactersAndTrims()
    {
        var s = PreLaunchSocialInputNormalizer.SanitizeName("  Ana\x00Silva  ");
        Assert.Equal("AnaSilva", s);
    }
}

public class PreLaunchPrivacyHashTests
{
    [Fact]
    public void Sha256Hex_IsDeterministicAndNotRawIp()
    {
        var h = PreLaunchPrivacyHash.Sha256Hex("203.0.113.9", "secret");
        Assert.Matches("^[a-f0-9]{64}$", h);
        Assert.DoesNotContain("203.0.113.9", h);
    }
}
