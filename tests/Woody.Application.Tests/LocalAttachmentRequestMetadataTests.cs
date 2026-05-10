using Woody.Application.Media;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public sealed class LocalAttachmentRequestMetadataTests
{
    private const string SampleGuid = "aabbccddeeff00112233445566778899";

    // ── URLs locais (comportamento anterior inalterado) ───────────────────────

    [Fact]
    public void LocalImageUrl_WithMatchingStorageKey_Resolves()
    {
        var key = $"posts/42/{SampleGuid}.jpg";
        var url = $"/api/media/images/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, "image/jpeg", null,
            100_000_000, 10_000_000,
            out var storageKey, out var mime, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal(key, storageKey);
        Assert.Equal("image/jpeg", mime);
    }

    [Fact]
    public void LocalVideoUrl_WithMatchingStorageKey_Resolves()
    {
        var key = $"posts/7/{SampleGuid}.mp4";
        var url = $"/api/media/videos/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Video, url, key, "video/mp4", null,
            100_000_000, 10_000_000,
            out var storageKey, out _, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal(key, storageKey);
    }

    [Fact]
    public void LocalImageUrl_WithoutStorageKey_Resolves()
    {
        var key = $"posts/42/{SampleGuid}.png";
        var url = $"/api/media/images/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, null, null, null,
            100_000_000, 10_000_000,
            out var storageKey, out _, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal(key, storageKey);
    }

    [Fact]
    public void LocalImageUrl_StorageKeyMismatch_Fails()
    {
        var url = $"/api/media/images/posts/42/{SampleGuid}.jpg";
        var wrongKey = $"posts/99/{SampleGuid}.jpg";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, wrongKey, null, null,
            100_000_000, 10_000_000,
            out _, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    // ── URLs CDN/R2 absolutas ─────────────────────────────────────────────────

    [Fact]
    public void CdnImageUrl_WithMatchingStorageKey_Resolves()
    {
        var key = $"posts/42/{SampleGuid}.jpg";
        var url = $"https://media-dev.thewoody.co/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, "image/jpeg", null,
            100_000_000, 10_000_000,
            out var storageKey, out var mime, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal(key, storageKey);
        Assert.Equal("image/jpeg", mime);
    }

    [Fact]
    public void CdnVideoUrl_WithMatchingStorageKey_Resolves()
    {
        var key = $"posts/7/{SampleGuid}.mp4";
        var url = $"https://media-dev.thewoody.co/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Video, url, key, "video/mp4", null,
            100_000_000, 10_000_000,
            out var storageKey, out _, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal(key, storageKey);
    }

    [Fact]
    public void CdnMessageImageUrl_WithMatchingStorageKey_Resolves()
    {
        var key = $"messages/99/{SampleGuid}.webp";
        var url = $"https://cdn.example.com/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, "image/webp", null,
            100_000_000, 10_000_000,
            out var storageKey, out _, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal(key, storageKey);
    }

    [Fact]
    public void CdnImageUrl_WithoutStorageKey_Resolves_NullKey()
    {
        var url = "https://media-dev.thewoody.co/external/image.jpg";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, null, null, null,
            100_000_000, 10_000_000,
            out var storageKey, out _, out _, out var error);

        Assert.True(ok, error);
        Assert.Null(storageKey);
    }

    [Fact]
    public void CdnImageUrl_StorageKeyNotInUrl_Fails()
    {
        var url = "https://media-dev.thewoody.co/posts/42/differentfile.jpg";
        var key = $"posts/42/{SampleGuid}.jpg";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, null, null,
            100_000_000, 10_000_000,
            out _, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void CdnImageUrl_InvalidStorageKeyFormat_Fails()
    {
        var url = "https://media-dev.thewoody.co/../../etc/passwd";
        var invalidKey = "../../etc/passwd";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, invalidKey, null, null,
            100_000_000, 10_000_000,
            out _, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void CdnImageUrl_MimeTypeMismatch_Fails()
    {
        var key = $"posts/42/{SampleGuid}.jpg";
        var url = $"https://media-dev.thewoody.co/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, "image/png", null,
            100_000_000, 10_000_000,
            out _, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void CdnImageUrl_WithFileSizeWithinLimit_Resolves()
    {
        var key = $"posts/42/{SampleGuid}.jpg";
        var url = $"https://media-dev.thewoody.co/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, "image/jpeg", 5_000_000,
            100_000_000, 10_000_000,
            out _, out _, out var resolvedSize, out var error);

        Assert.True(ok, error);
        Assert.Equal(5_000_000, resolvedSize);
    }

    [Fact]
    public void CdnImageUrl_FileSizeExceedsLimit_Fails()
    {
        var key = $"posts/42/{SampleGuid}.jpg";
        var url = $"https://media-dev.thewoody.co/{key}";

        var ok = LocalAttachmentRequestMetadata.TryResolve(
            MediaKind.Image, url, key, null, 20_000_000,
            100_000_000, 10_000_000,
            out _, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
