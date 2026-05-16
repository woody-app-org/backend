using Woody.Application.Validation;

namespace Woody.Application.Tests;

public sealed class CommentGifAttachmentValidatorTests
{
    private const string ValidHttpsGif =
        "https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif";

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/gif;base64,R0lGODlhAQABAAAAACw=")]
    [InlineData("blob:https://example.com/uuid")]
    [InlineData("file:///etc/passwd")]
    public void TryNormalizeStrictExternalHttpsGifUrl_Rejects_schemes(string raw)
    {
        var ok = CommentGifAttachmentValidator.TryNormalizeStrictExternalHttpsGifUrl(raw, out _, out var err);
        Assert.False(ok);
        Assert.NotNull(err);
    }

    [Fact]
    public void TryNormalizeGifFields_Accepts_https_gif_and_optional_thumbnail()
    {
        var ok = CommentGifAttachmentValidator.TryNormalizeGifFields(
            ValidHttpsGif,
            "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2c/Rotating_earth_%28large%29.gif/120px-Rotating_earth_%28large%29.gif",
            "local_catalog",
            "wm-earth",
            "Terra",
            out var url,
            out var thumb,
            out var prov,
            out var ext,
            out var title,
            out var err);

        Assert.True(ok);
        Assert.Null(err);
        Assert.Equal(ValidHttpsGif, url);
        Assert.NotNull(thumb);
        Assert.Equal("local_catalog", prov);
        Assert.Equal("wm-earth", ext);
        Assert.Equal("Terra", title);
    }

    [Fact]
    public void TryNormalizeGifFields_Rejects_unknown_provider()
    {
        var ok = CommentGifAttachmentValidator.TryNormalizeGifFields(
            ValidHttpsGif,
            null,
            "evil_cdn",
            "x",
            null,
            out _,
            out _,
            out _,
            out _,
            out _,
            out var err);

        Assert.False(ok);
        Assert.Contains("Provedor", err, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://media.tenor.com/abc.gif?itemid=123")]
    [InlineData("https://media.giphy.com/media/abc.gif?cid=xyz&rid=ok")]
    public void TryNormalizeStrictExternalHttpsGifUrl_Accepts_gif_with_query_string(string raw)
    {
        var ok = CommentGifAttachmentValidator.TryNormalizeStrictExternalHttpsGifUrl(raw, out var normalized, out var err);
        Assert.True(ok, err);
        Assert.Equal(raw, normalized);
    }

    [Theory]
    [InlineData("https://cdn.example.com/image.png")]
    [InlineData("https://cdn.example.com/video.mp4?token=abc")]
    [InlineData("https://cdn.example.com/file.webp")]
    public void TryNormalizeStrictExternalHttpsGifUrl_Rejects_non_gif_paths(string raw)
    {
        var ok = CommentGifAttachmentValidator.TryNormalizeStrictExternalHttpsGifUrl(raw, out _, out var err);
        Assert.False(ok);
        Assert.NotNull(err);
    }

    [Fact]
    public void TryNormalizeGifFields_Rejects_title_with_angle_brackets()
    {
        var ok = CommentGifAttachmentValidator.TryNormalizeGifFields(
            ValidHttpsGif,
            null,
            "klipy",
            "abc",
            "a <b> c",
            out _,
            out _,
            out _,
            out _,
            out _,
            out var err);

        Assert.False(ok);
        Assert.Contains("HTML", err, StringComparison.Ordinal);
    }
}
