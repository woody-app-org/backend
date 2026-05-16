using Woody.Application.Mapping;
using Woody.Domain.Entities;

namespace Woody.Application.Tests;

public sealed class EntityMappersCommentGifTests
{
    private const string WikimediaGif =
        "https://upload.wikimedia.org/wikipedia/commons/2/2c/Rotating_earth_%28large%29.gif";

    private static User CommentAuthor(int id = 2) => new()
    {
        Id = id,
        Username = "cauthor",
        DisplayName = "Commenter",
        Email = "c@example.test",
        Role = "User",
    };

    [Fact]
    public void ToCommentDto_WhenHiddenForViewer_Omits_Gif_and_content()
    {
        var author = CommentAuthor();
        var c = new Comment
        {
            Id = 1,
            PostId = 5,
            AuthorId = author.Id,
            Author = author,
            Content = "secret",
            CreatedAt = DateTime.UtcNow,
            HiddenByPostAuthorAt = DateTime.UtcNow,
            GifUrl = WikimediaGif,
            GifProvider = "local_catalog",
            GifExternalId = "wm-earth",
            GifTitle = "Earth",
        };

        var dto = EntityMappers.ToCommentDto(c, postAuthorId: 1, viewerUserId: 99, 0, false);

        Assert.Empty(dto.Content);
        Assert.Null(dto.Gif);
        Assert.Equal("hidden_by_post_author", dto.ContentModerationMask);
    }

    [Fact]
    public void ToCommentDto_WhenVisible_Includes_Gif_payload()
    {
        var author = CommentAuthor();
        var c = new Comment
        {
            Id = 1,
            PostId = 5,
            AuthorId = author.Id,
            Author = author,
            Content = "olá",
            CreatedAt = DateTime.UtcNow,
            GifUrl = WikimediaGif,
            GifThumbnailUrl = null,
            GifProvider = "local_catalog",
            GifExternalId = "wm-earth",
            GifTitle = "Planeta",
        };

        var dto = EntityMappers.ToCommentDto(c, postAuthorId: 1, viewerUserId: 99, 3, true);

        Assert.Equal("olá", dto.Content);
        Assert.NotNull(dto.Gif);
        Assert.Equal(WikimediaGif, dto.Gif!.Url);
        Assert.Equal("local_catalog", dto.Gif.Provider);
        Assert.Equal("wm-earth", dto.Gif.ExternalId);
        Assert.Equal("Planeta", dto.Gif.Title);
    }
}
