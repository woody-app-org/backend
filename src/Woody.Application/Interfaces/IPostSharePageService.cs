using Woody.Application.Posts;

namespace Woody.Application.Interfaces;

public interface IPostSharePageService
{
    Task<PostSharePageModel> BuildPageModelAsync(
        string publicId,
        string requestOrigin,
        CancellationToken cancellationToken = default);

    string RenderHtml(PostSharePageModel model);
}
