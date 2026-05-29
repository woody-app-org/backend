using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;

namespace Woody.Application.Tests;

public class PostSharePageServiceTests
{
    private const string RequestOrigin = "https://api.woody.test";
    private const string FrontendOrigin = "https://app.woody.test";

    [Fact]
    public async Task BuildPageModelAsync_ReturnsRealPreview_ForPublicProfilePost()
    {
        var post = SampleProfilePost("pst_public0001", content: "Uma publicação pública interessante");
        post.MediaAttachments.Add(new MediaAttachment
        {
            Id = 1,
            MediaKind = MediaKind.Image,
            Url = "/api/media/images/abc.jpg",
            DisplayOrder = 0
        });

        var svc = CreateService(post, canRead: true);

        var model = await svc.BuildPageModelAsync("pst_public0001", RequestOrigin, CancellationToken.None);

        Assert.False(model.IsUnavailable);
        Assert.Contains("publicação pública", model.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://api.woody.test/api/media/images/abc.jpg", model.ImageUrl);
        Assert.Equal($"{RequestOrigin}/share/posts/pst_public0001", model.SharePageUrl);
        Assert.Equal($"{FrontendOrigin}/posts/pst_public0001", model.FrontendPostUrl);
    }

    [Fact]
    public async Task BuildPageModelAsync_ReturnsGenericPreview_ForPrivateCommunityPost()
    {
        var post = SampleCommunityPost("pst_private001", visibility: "private");
        var svc = CreateService(post, canRead: false);

        var model = await svc.BuildPageModelAsync("pst_private001", RequestOrigin, CancellationToken.None);

        Assert.True(model.IsUnavailable);
        Assert.Equal("Woody", model.Title);
        Assert.Equal("Conteúdo disponível apenas para quem tem acesso.", model.Description);
        Assert.Equal($"{FrontendOrigin}/icon-512.png", model.ImageUrl);
    }

    [Fact]
    public async Task BuildPageModelAsync_ReturnsGenericPreview_WhenPostDeleted()
    {
        var svc = CreateService(post: null, canRead: false);

        var model = await svc.BuildPageModelAsync("pst_missing001", RequestOrigin, CancellationToken.None);

        Assert.True(model.IsUnavailable);
        Assert.Equal("Woody", model.Title);
    }

    [Fact]
    public async Task BuildPageModelAsync_TruncatesLongDescription()
    {
        var longText = new string('a', 200);
        var post = SampleProfilePost("pst_long00001", content: longText);
        var svc = CreateService(post, canRead: true);

        var model = await svc.BuildPageModelAsync("pst_long00001", RequestOrigin, CancellationToken.None);

        Assert.False(model.IsUnavailable);
        Assert.True(model.Description.Length <= 161);
        Assert.EndsWith("…", model.Description);
    }

    [Fact]
    public async Task BuildPageModelAsync_UsesVideoThumbnail_WhenAvailable()
    {
        var post = SampleProfilePost("pst_video0001", content: "vídeo");
        post.MediaAttachments.Add(new MediaAttachment
        {
            Id = 2,
            MediaKind = MediaKind.Video,
            Url = "/api/media/videos/vid.mp4",
            ThumbnailUrl = "https://cdn.woody.test/thumb.jpg",
            DisplayOrder = 0
        });

        var svc = CreateService(post, canRead: true);
        var model = await svc.BuildPageModelAsync("pst_video0001", RequestOrigin, CancellationToken.None);

        Assert.Equal("https://cdn.woody.test/thumb.jpg", model.ImageUrl);
    }

    [Fact]
    public async Task BuildPageModelAsync_UsesFallbackImage_WhenNoMedia()
    {
        var post = SampleProfilePost("pst_nomedia01", content: "só texto");
        var svc = CreateService(post, canRead: true);

        var model = await svc.BuildPageModelAsync("pst_nomedia01", RequestOrigin, CancellationToken.None);

        Assert.Equal($"{FrontendOrigin}/icon-512.png", model.ImageUrl);
    }

    private static PostSharePageService CreateService(Post? post, bool canRead)
    {
        var posts = new Mock<IPostRepository>();
        posts
            .Setup(x => x.GetByPublicIdNonDeletedWithNavAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
                post != null && string.Equals(post.PublicId, id, StringComparison.Ordinal) ? post : null);

        var auth = new Mock<IResourceAuthorizationService>();
        auth
            .Setup(x => x.CanReadPostAsync(It.IsAny<Post>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post p, int? _, CancellationToken _) => canRead && p == post);

        var options = Options.Create(new PublicShareOptions
        {
            FrontendPublicOrigin = FrontendOrigin,
            OgFallbackImageUrl = ""
        });

        return new PostSharePageService(posts.Object, auth.Object, options);
    }

    private static Post SampleProfilePost(string publicId, string content)
    {
        var user = new User
        {
            Id = 1,
            Username = "camila",
            DisplayName = "Camila",
            Email = "c@t.com",
            VerificationStatus = VerificationStatus.Approved
        };
        return new Post
        {
            Id = 10,
            PublicId = publicId,
            UserId = user.Id,
            User = user,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            PublicationContext = PostPublicationContext.Profile
        };
    }

    private static Post SampleCommunityPost(string publicId, string visibility)
    {
        var post = SampleProfilePost(publicId, "secret");
        post.PublicationContext = PostPublicationContext.Community;
        post.CommunityId = 5;
        post.Community = new Community
        {
            Id = 5,
            Name = "Priv",
            Slug = "priv",
            Visibility = visibility,
            OwnerUserId = 1
        };
        return post;
    }
}
