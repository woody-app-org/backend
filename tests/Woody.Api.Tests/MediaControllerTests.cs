using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Media;
using Woody.Infrastructure.Storage;

namespace Woody.Api.Tests;

public class MediaControllerTests
{
    [Fact]
    public async Task UploadImage_StoresValidPngWithServerGeneratedName()
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var file = CreateFormFile("original.png", "image/png", ValidPng());

        var actionResult = await controller.UploadImage(CreatePostProfileForm(file), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(actionResult);
        var dto = Assert.IsType<MediaUploadResponseDto>(created.Value);
        Assert.EndsWith(".png", dto.StorageKey);
        Assert.DoesNotContain("original", dto.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("/api/media/images/", dto.Url, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(fixture.RootPath, dto.StorageKey)));
    }

    [Fact]
    public async Task UploadImage_RejectsOversizedFile()
    {
        using var fixture = MediaControllerFixture.CreateWithMaxImageBytes(16);
        var controller = fixture.CreateController();
        var file = CreateFormFile("large.png", "image/png", ValidPng().Concat(new byte[64]).ToArray());

        var actionResult = await controller.UploadImage(CreatePostProfileForm(file), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task UploadImage_RejectsMissingScope()
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var file = CreateFormFile("image.png", "image/png", ValidPng());

        var actionResult = await controller.UploadImage(new ScopedMediaUploadForm { File = file }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task UploadImage_CommunityForbidden_WhenCannotPublish()
    {
        using var fixture = MediaControllerFixture.CreateWithCommunityPermission(false);
        var controller = fixture.CreateController();
        var file = CreateFormFile("image.png", "image/png", ValidPng());

        var form = new ScopedMediaUploadForm
        {
            File = file,
            Scope = "post",
            PublicationContext = "community",
            CommunityId = "9"
        };

        var actionResult = await controller.UploadImage(form, CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(actionResult).StatusCode);
    }

    [Theory]
    [InlineData("image.svg", "image/svg+xml")]
    [InlineData("image.html", "text/html")]
    [InlineData("image.js", "application/javascript")]
    [InlineData("image.exe", "application/octet-stream")]
    [InlineData("image.png.jpg", "image/jpeg")]
    public async Task UploadImage_RejectsUnsafeExtensions(string fileName, string contentType)
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var file = CreateFormFile(fileName, contentType, ValidPng());

        var actionResult = await controller.UploadImage(CreatePostProfileForm(file), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task UploadImage_RejectsMimeMismatch()
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var file = CreateFormFile("image.png", "image/jpeg", ValidPng());

        var actionResult = await controller.UploadImage(CreatePostProfileForm(file), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task UploadImage_RejectsInvalidMagicBytes()
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var file = CreateFormFile("image.png", "image/png", "<html>not an image</html>"u8.ToArray());

        var actionResult = await controller.UploadImage(CreatePostProfileForm(file), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task UploadImage_RejectsPathTraversalFileName()
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var file = CreateFormFile("../image.png", "image/png", ValidPng());

        var actionResult = await controller.UploadImage(CreatePostProfileForm(file), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetImage_ReturnsSafeContentHeaders()
    {
        using var fixture = MediaControllerFixture.CreateDefault();
        var controller = fixture.CreateController();
        var uploadResult = await controller.UploadImage(
            CreatePostProfileForm(CreateFormFile("image.webp", "image/webp", ValidWebp())),
            CancellationToken.None);
        var created = Assert.IsType<CreatedResult>(uploadResult);
        var dto = Assert.IsType<MediaUploadResponseDto>(created.Value);

        var downloadResult = await controller.GetImage(dto.StorageKey, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(downloadResult);
        Assert.Equal("image/webp", fileResult.ContentType);
        Assert.Equal("nosniff", controller.Response.Headers.XContentTypeOptions);
        Assert.Equal("inline; filename=\"image\"", controller.Response.Headers.ContentDisposition);
        await fileResult.FileStream.DisposeAsync();
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static ScopedMediaUploadForm CreatePostProfileForm(IFormFile file) => new()
    {
        File = file,
        Scope = "post",
        PublicationContext = "profile"
    };

    private static byte[] ValidPng() =>
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };

    private static byte[] ValidWebp() =>
        new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x00 };

    private sealed class MediaControllerFixture : IDisposable
    {
        private readonly MediaStorageOptions _options;
        private readonly Mock<ICommunityPermissionService> _communityPermissions = new();
        private readonly Mock<IConversationRepository> _conversations = new();

        private MediaControllerFixture(
            long maxImageSizeBytes,
            bool communityCanPublish)
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"woody-media-tests-{Guid.NewGuid():N}");
            _options = new MediaStorageOptions
            {
                RootPath = RootPath,
                PublicUrlPath = "/api/media/images",
                MaxImageSizeBytes = maxImageSizeBytes
            };
            _communityPermissions
                .Setup(x => x.CanPublishPostAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(communityCanPublish);
            _conversations
                .Setup(x => x.IsParticipantAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        public string RootPath { get; }

        public static MediaControllerFixture CreateDefault() => new(
            maxImageSizeBytes: MediaReferenceConstraints.ImageMaxUploadBytes,
            communityCanPublish: true);

        public static MediaControllerFixture CreateWithMaxImageBytes(long maxImageSizeBytes) => new(
            maxImageSizeBytes,
            communityCanPublish: true);

        public static MediaControllerFixture CreateWithCommunityPermission(bool canPublish) => new(
            maxImageSizeBytes: MediaReferenceConstraints.ImageMaxUploadBytes,
            communityCanPublish: canPublish);

        public MediaController CreateController()
        {
            var options = Options.Create(_options);
            var storage = new LocalMediaStorage(options);
            var core = new MediaUploadService(storage, options);
            var app = new MediaUploadApplicationService(core, _communityPermissions.Object, _conversations.Object, options);
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
                authenticationType: "Test"));
            return new MediaController(app, storage)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
    }
}
