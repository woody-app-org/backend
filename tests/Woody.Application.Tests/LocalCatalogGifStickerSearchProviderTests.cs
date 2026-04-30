using Woody.Application.Services.Messaging;

namespace Woody.Application.Tests;

public sealed class LocalCatalogGifStickerSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_empty_query_returns_items()
    {
        var sut = new LocalCatalogGifStickerSearchProvider();
        var r = await sut.SearchAsync(null, 10, CancellationToken.None);
        Assert.Equal("local_catalog", r.ProviderKey);
        Assert.NotEmpty(r.Items);
    }

    [Fact]
    public async Task SearchAsync_filters_by_title_token()
    {
        var sut = new LocalCatalogGifStickerSearchProvider();
        var r = await sut.SearchAsync("terra", 10, CancellationToken.None);
        Assert.Contains(r.Items, x => x.ExternalId == "wm-earth");
    }
}
