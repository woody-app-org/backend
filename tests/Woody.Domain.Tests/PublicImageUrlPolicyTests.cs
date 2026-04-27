using Woody.Domain.Media;

namespace Woody.Domain.Tests;

public class PublicImageUrlPolicyTests
{
    [Theory]
    [InlineData("https://cdn.example.com/image.png")]
    [InlineData("https://images.example.org/path/photo.webp?size=large")]
    public void IsPermittedExternalImageUrl_AllowsHttpsUrls(string url)
    {
        Assert.True(PublicImageUrlPolicy.IsPermittedExternalImageUrl(url));
    }

    [Theory]
    [InlineData("http://cdn.example.com/image.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,abc")]
    [InlineData("https://localhost/image.png")]
    [InlineData("https://127.0.0.1/image.png")]
    [InlineData("https://192.168.0.10/image.png")]
    [InlineData("https://cdn.example.com/image.png\r\nx: y")]
    public void IsPermittedExternalImageUrl_RejectsUnsafeUrls(string url)
    {
        Assert.False(PublicImageUrlPolicy.IsPermittedExternalImageUrl(url));
    }

    [Fact]
    public void IsPermittedExternalImageUrl_RejectsOverlongUrls()
    {
        var url = $"https://cdn.example.com/{new string('a', PublicImageUrlPolicy.MaxUrlLength)}.png";

        Assert.False(PublicImageUrlPolicy.IsPermittedExternalImageUrl(url));
    }
}
