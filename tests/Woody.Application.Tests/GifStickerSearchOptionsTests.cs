using Woody.Application.Configuration;

namespace Woody.Application.Tests;

public sealed class GifStickerSearchOptionsTests
{
    [Fact]
    public void GetResolvedKind_empty_defaults_Local_and_valid()
    {
        var o = new GifStickerSearchOptions { Provider = "" };
        var k = o.GetResolvedKind(out var invalid);
        Assert.Equal(GifStickerSearchProviderKind.Local, k);
        Assert.False(invalid);
    }

    [Fact]
    public void GetResolvedKind_Klipy_case_insensitive()
    {
        var o = new GifStickerSearchOptions { Provider = "klipy" };
        var k = o.GetResolvedKind(out var invalid);
        Assert.Equal(GifStickerSearchProviderKind.Klipy, k);
        Assert.False(invalid);
    }

    [Fact]
    public void GetResolvedKind_unknown_falls_back_Local_and_flags_invalid()
    {
        var o = new GifStickerSearchOptions { Provider = "Tenor" };
        var k = o.GetResolvedKind(out var invalid);
        Assert.Equal(GifStickerSearchProviderKind.Local, k);
        Assert.True(invalid);
    }
}
