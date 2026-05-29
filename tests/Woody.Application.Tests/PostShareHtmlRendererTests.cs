using Woody.Application.Posts;

namespace Woody.Application.Tests;

public class PostShareHtmlRendererTests
{
    [Fact]
    public void Render_EscapesHtmlInMetaTags()
    {
        var html = PostShareHtmlRenderer.Render(new PostSharePageModel
        {
            Title = "Título <script>alert(1)</script>",
            Description = "Desc & \"quotes\"",
            ImageUrl = "https://cdn.test/img.jpg",
            SharePageUrl = "https://api.test/share/posts/pst_x",
            FrontendPostUrl = "https://app.test/posts/pst_x",
            IsUnavailable = false
        });

        Assert.Contains("og:title", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.Contains("Desc &amp; &quot;quotes&quot;", html);
    }

    [Fact]
    public void Render_IncludesOgAndTwitterTags()
    {
        var html = PostShareHtmlRenderer.Render(SampleModel());

        Assert.Contains("property=\"og:title\"", html);
        Assert.Contains("property=\"og:description\"", html);
        Assert.Contains("property=\"og:image\"", html);
        Assert.Contains("property=\"og:url\"", html);
        Assert.Contains("name=\"twitter:card\"", html);
        Assert.Contains("name=\"twitter:image\"", html);
        Assert.Contains("Abrir na Woody", html);
    }

    [Fact]
    public void Render_ShowsUnavailableMessage_ForGenericPreview()
    {
        var html = PostShareHtmlRenderer.Render(new PostSharePageModel
        {
            Title = "Woody",
            Description = "Conteúdo disponível apenas para quem tem acesso.",
            ImageUrl = "https://app.test/icon-512.png",
            SharePageUrl = "https://api.test/share/posts/pst_private",
            FrontendPostUrl = "https://app.test/posts/pst_private",
            IsUnavailable = true,
            PublicId = "pst_private"
        });

        Assert.Contains("Este conteúdo não está disponível.", html);
        Assert.Contains("Conteúdo disponível apenas para quem tem acesso na Woody.", html);
    }

    private static PostSharePageModel SampleModel() =>
        new()
        {
            Title = "Olá Woody",
            Description = "Preview curto",
            ImageUrl = "https://cdn.test/img.jpg",
            SharePageUrl = "https://api.test/share/posts/pst_abc",
            FrontendPostUrl = "https://app.test/posts/pst_abc",
            IsUnavailable = false,
            PublicId = "pst_abc"
        };
}
