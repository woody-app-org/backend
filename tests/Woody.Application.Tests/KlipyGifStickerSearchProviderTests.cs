using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Configuration;
using Woody.Application.Services.Messaging;

namespace Woody.Application.Tests;

public sealed class KlipyGifStickerSearchProviderTests
{
    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _queue = new();

        public void Enqueue(HttpResponseMessage response) => _queue.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_queue.Count == 0)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(_queue.Dequeue());
        }
    }

    [Fact]
    public async Task SearchAsync_maps_gif_and_sticker_and_interleaves()
    {
        const string gifBody =
            """{"result":true,"data":{"data":[{"id":1,"slug":"hello","title":"Hello","file":{"hd":{"gif":{"url":"https://static.klipy.com/a.gif"},"jpg":{"url":"https://static.klipy.com/a.jpg"}}}}]}}""";
        const string stickerBody =
            """{"result":true,"data":{"data":[{"id":2,"slug":"st","title":"Sticker","file":{"hd":{"webp":{"url":"https://static.klipy.com/b.webp"}}}}]}}""";

        var handler = new QueueHttpMessageHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(gifBody, Encoding.UTF8, "application/json"),
        });
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(stickerBody, Encoding.UTF8, "application/json"),
        });

        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(f => f.CreateClient(KlipyGifStickerSearchProvider.HttpClientName))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        var options = Options.Create(new GifStickerSearchOptions
        {
            Klipy = new GifStickerSearchKlipyOptions
            {
                ApiKey = "unit-test-key",
                BaseUrl = "https://api.klipy.com/",
            },
            EnableFallbackToLocal = false,
        });

        var sut = new KlipyGifStickerSearchProvider(
            factory.Object,
            options,
            new LocalCatalogGifStickerSearchProvider(),
            NullLogger<KlipyGifStickerSearchProvider>.Instance);

        var r = await sut.SearchAsync(null, 4, CancellationToken.None);

        Assert.Equal("klipy", r.ProviderKey);
        Assert.Equal(2, r.Items.Count);
        Assert.Equal("gif", r.Items[0].MediaType);
        Assert.Equal("https://static.klipy.com/a.gif", r.Items[0].Url);
        Assert.Equal("klipy", r.Items[0].Provider);
        Assert.Equal("hello", r.Items[0].ExternalId);
        Assert.Equal("sticker", r.Items[1].MediaType);
        Assert.Equal("https://static.klipy.com/b.webp", r.Items[1].Url);
    }

    [Fact]
    public async Task SearchAsync_without_api_key_falls_back_to_local_when_enabled()
    {
        var factory = new Mock<IHttpClientFactory>();
        var options = Options.Create(new GifStickerSearchOptions
        {
            Klipy = new GifStickerSearchKlipyOptions { ApiKey = "" },
            EnableFallbackToLocal = true,
        });

        var sut = new KlipyGifStickerSearchProvider(
            factory.Object,
            options,
            new LocalCatalogGifStickerSearchProvider(),
            NullLogger<KlipyGifStickerSearchProvider>.Instance);

        var r = await sut.SearchAsync(null, 10, CancellationToken.None);

        factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        Assert.Equal("klipy", r.ProviderKey);
        Assert.NotEmpty(r.Items);
        Assert.Contains(r.Items, x => x.Url.Contains("upload.wikimedia.org", StringComparison.Ordinal));
    }
}
