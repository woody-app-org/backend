using Woody.Application.Utilities;
using Woody.Domain.Entities;

namespace Woody.Application.Tests;

public class CommentBlockFilterTests
{
    [Fact]
    public void FilterForViewer_ExcludesCommentsFromHiddenAuthors()
    {
        var comments = new List<Comment>
        {
            CreateComment(1, authorId: 5, parentId: null),
            CreateComment(2, authorId: 9, parentId: null)
        };

        var filtered = CommentBlockFilter.FilterForViewer(comments, new HashSet<int> { 5 });

        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].Id);
    }

    [Fact]
    public void FilterForViewer_ExcludesRepliesWhenParentAuthorHidden()
    {
        var comments = new List<Comment>
        {
            CreateComment(1, authorId: 5, parentId: null),
            CreateComment(2, authorId: 9, parentId: 1)
        };

        var filtered = CommentBlockFilter.FilterForViewer(comments, new HashSet<int> { 5 });

        Assert.Empty(filtered);
    }

    [Fact]
    public void FilterForViewer_ExcludesRepliesWhenParentCommentHidden()
    {
        var comments = new List<Comment>
        {
            CreateComment(1, authorId: 5, parentId: null),
            CreateComment(2, authorId: 9, parentId: 1),
            CreateComment(3, authorId: 9, parentId: null)
        };

        var filtered = CommentBlockFilter.FilterForViewer(comments, new HashSet<int> { 5 });

        Assert.Single(filtered);
        Assert.Equal(3, filtered[0].Id);
    }

    private static Comment CreateComment(int id, int authorId, int? parentId) =>
        new()
        {
            Id = id,
            PostId = 100,
            AuthorId = authorId,
            ParentCommentId = parentId,
            Content = "x",
            CreatedAt = DateTime.UtcNow,
            Author = new User { Id = authorId, Username = $"u{authorId}" }
        };
}
