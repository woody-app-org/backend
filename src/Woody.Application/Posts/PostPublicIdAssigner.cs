using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Application.Posts;

public static class PostPublicIdAssigner
{
    private const int MaxAttempts = 8;

    public static async Task AssignUniqueAsync(
        IPostRepository posts,
        Post post,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            post.PublicId = PostPublicIdGenerator.Generate();
            if (!await posts.ExistsPublicIdAsync(post.PublicId, cancellationToken))
                return;
        }

        throw new InvalidOperationException("Não foi possível gerar PublicId único para o post.");
    }
}
