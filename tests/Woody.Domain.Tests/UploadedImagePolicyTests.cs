using Woody.Domain.Media;

namespace Woody.Domain.Tests;

public class UploadedImagePolicyTests
{
    [Theory]
    [InlineData("photo.jpg", "image/jpeg", ".jpg")]
    [InlineData("photo.jpeg", "image/jpeg", ".jpeg")]
    [InlineData("photo.png", "image/png", ".png")]
    [InlineData("photo.webp", "image/webp", ".webp")]
    public void ValidateMetadata_AllowsSupportedImages(string fileName, string contentType, string extension)
    {
        var result = UploadedImagePolicy.ValidateMetadata(fileName, contentType, sizeBytes: 100);

        Assert.True(result.IsValid);
        Assert.Equal(extension, result.Extension);
        Assert.Equal(contentType, result.ContentType);
    }

    [Theory]
    [InlineData("photo.svg", "image/svg+xml")]
    [InlineData("photo.html", "text/html")]
    [InlineData("photo.js", "application/javascript")]
    [InlineData("photo.exe", "application/octet-stream")]
    [InlineData("photo.png.jpg", "image/jpeg")]
    [InlineData("../photo.png", "image/png")]
    public void ValidateMetadata_RejectsUnsafeNamesAndExtensions(string fileName, string contentType)
    {
        var result = UploadedImagePolicy.ValidateMetadata(fileName, contentType, sizeBytes: 100);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMetadata_RejectsMimeMismatch()
    {
        var result = UploadedImagePolicy.ValidateMetadata("photo.png", "image/jpeg", sizeBytes: 100);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMetadata_RejectsOversizedFile()
    {
        var result = UploadedImagePolicy.ValidateMetadata(
            "photo.png",
            "image/png",
            sizeBytes: UploadedImagePolicy.DefaultMaxSizeBytes + 1);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }, ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ".png")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 }, ".webp")]
    public void HasValidMagicBytes_AcceptsKnownSignatures(byte[] bytes, string extension)
    {
        Assert.True(UploadedImagePolicy.HasValidMagicBytes(bytes, extension));
    }

    [Fact]
    public void HasValidMagicBytes_RejectsFakeImage()
    {
        var htmlBytes = "<html><script>alert(1)</script></html>"u8.ToArray();

        Assert.False(UploadedImagePolicy.HasValidMagicBytes(htmlBytes, ".png"));
    }
}
