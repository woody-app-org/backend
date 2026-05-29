using Woody.Domain.Entities;

namespace Woody.Application.Utilities;

/// <summary>
/// Remove comentários de utilizadoras ocultas por bloqueio e subárvores cujo pai ficou oculto.
/// </summary>
public static class CommentBlockFilter
{
    public static List<Comment> FilterForViewer(IReadOnlyList<Comment> comments, IReadOnlySet<int> hiddenAuthorIds)
    {
        if (hiddenAuthorIds.Count == 0 || comments.Count == 0)
            return comments.ToList();

        var visibleIds = new HashSet<int>();
        foreach (var c in comments
                     .OrderBy(x => x.ParentCommentId == null ? 0 : 1)
                     .ThenBy(x => x.CreatedAt)
                     .ThenBy(x => x.Id))
        {
            if (hiddenAuthorIds.Contains(c.AuthorId))
                continue;

            if (c.ParentCommentId is int parentId && !visibleIds.Contains(parentId))
                continue;

            visibleIds.Add(c.Id);
        }

        return comments.Where(c => visibleIds.Contains(c.Id)).ToList();
    }
}
