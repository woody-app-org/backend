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
    }
}
