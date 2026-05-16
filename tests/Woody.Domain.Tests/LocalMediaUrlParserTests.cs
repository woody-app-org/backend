using Woody.Domain.Media;

namespace Woody.Domain.Tests;

public sealed class LocalMediaUrlParserTests
{
    [Fact]
    public void TryGetImageKey_FromStructuredLocalUrl()
    {
        var url = "/api/media/images/posts/42/aabbccddeeff00112233445566778899.png";
        Assert.True(LocalMediaUrlParser.TryGetImageStorageKeyFromLocalUrl(url, out var key));
        Assert.Equal("posts/42/aabbccddeeff00112233445566778899.png", key);
    }

    [Fact]
    public void TryGetImageKey_FromLegacyFlatLocalUrl()
    {
        var url = "/api/media/images/aabbccddeeff00112233445566778899.webp";
        Assert.True(LocalMediaUrlParser.TryGetImageStorageKeyFromLocalUrl(url, out var key));
        Assert.Equal("aabbccddeeff00112233445566778899.webp", key);
    }

    [Fact]
    public void TryGetImageKey_RejectsQueryString()
    {
        Assert.False(LocalMediaUrlParser.TryGetImageStorageKeyFromLocalUrl(
            "/api/media/images/posts/1/aabbccddeeff00112233445566778899.png?x=1",
            out _));
    }

    [Fact]
    public void TryGetVideoKey_FromStructuredLocalUrl()
    {
        var url = "/api/media/videos/messages/9/aabbccddeeff00112233445566778899.mp4";
        Assert.True(LocalMediaUrlParser.TryGetVideoStorageKeyFromLocalUrl(url, out var key));
        Assert.Equal("messages/9/aabbccddeeff00112233445566778899.mp4", key);
    }
}
