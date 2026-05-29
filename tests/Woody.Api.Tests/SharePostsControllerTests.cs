using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.Interfaces;
using Woody.Application.Posts;

namespace Woody.Api.Tests;

public class SharePostsControllerTests
{
    [Fact]
    public async Task GetSharePage_ReturnsHtml_WithOgTags()
    {
        var model = new PostSharePageModel
        {
            Title = "Publicação de Camila na Woody",
            Description = "Olá mundo",
            ImageUrl = "https://cdn.test/img.jpg",
            SharePageUrl = "https://api.test/share/posts/pst_abc",
            FrontendPostUrl = "https://app.test/posts/pst_abc",
            IsUnavailable = false,
            PublicId = "pst_abc"
        };

        var sharePages = new Mock<IPostSharePageService>();
        sharePages
            .Setup(x => x.BuildPageModelAsync("pst_abc", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        sharePages.Setup(x => x.RenderHtml(model)).Returns("<html><meta property=\"og:title\" content=\"x\"></html>");

        var controller = new SharePostsController(sharePages.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Request = { Scheme = "https", Host = new HostString("api.test") }
                }
            }
        };

        var result = await controller.GetSharePage("pst_abc", CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html; charset=utf-8", content.ContentType);
        Assert.Contains("og:title", content.Content);
    }
}
