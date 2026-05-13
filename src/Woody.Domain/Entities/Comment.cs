namespace Woody.Domain.Entities
{
    public class Comment
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int AuthorId { get; set; }
        public User Author { get; set; } = null!;

        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? HiddenByPostAuthorAt { get; set; }

        /// <summary>Quando não nulo, o comentário está destacado neste post (no máximo um por post).</summary>
        public DateTime? PinnedOnPostAt { get; set; }

        /// <summary>GIF externo opcional (no máximo um por comentário); URLs só https públicas validadas na API.</summary>
        public string? GifUrl { get; set; }

        public string? GifThumbnailUrl { get; set; }
        public string? GifProvider { get; set; }
        public string? GifExternalId { get; set; }
        public string? GifTitle { get; set; }
    }
}
