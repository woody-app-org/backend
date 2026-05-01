using Woody.Domain.Media;

namespace Woody.Domain.Tests;

public class UploadedVideoPolicyTests
{
    [Theory]
    [InlineData("clip.mp4", "video/mp4")]
    [InlineData("My.Screen.Recording.2024.mp4", "video/mp4")]
    [InlineData("holiday.mov", "video/quicktime")]
    [InlineData("clip.webm", "video/webm")]
    [InlineData("itunes.m4v", "video/mp4")]
    public void ValidateMetadata_AllowsDotsInBaseName(string fileName, string contentType)
    {
        var result = UploadedVideoPolicy.ValidateMetadata(
            fileName,
            contentType,
            sizeBytes: 1024,
            maxSizeBytes: UploadedVideoPolicy.DefaultMaxSizeBytes);

        Assert.True(result.IsValid);
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void ValidateMetadata_RejectsWhenInnerExtensionIsDangerous()
    {
        var result = UploadedVideoPolicy.ValidateMetadata(
            "payload.exe.mp4",
            "video/mp4",
            sizeBytes: 1024,
            maxSizeBytes: UploadedVideoPolicy.DefaultMaxSizeBytes);

        Assert.False(result.IsValid);
        Assert.Equal("Extensão de arquivo inválida.", result.Error);
    }
}
