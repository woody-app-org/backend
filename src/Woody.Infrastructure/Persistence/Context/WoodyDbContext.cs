using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;

namespace Woody.Infrastructure.Persistence.Context
{
    public class WoodyDbContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Post> Posts { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<Like> Likes { get; set; }
        public virtual DbSet<Follow> Follows { get; set; }
        public virtual DbSet<Community> Communities { get; set; }
        public virtual DbSet<CommunityTag> CommunityTags { get; set; }
        public virtual DbSet<CommunityMembership> CommunityMemberships { get; set; }
        public virtual DbSet<JoinRequest> JoinRequests { get; set; }
        public virtual DbSet<ContentReport> ContentReports { get; set; }
        public virtual DbSet<UserSocialLink> UserSocialLinks { get; set; }
        public virtual DbSet<UserInterest> UserInterests { get; set; }
        public virtual DbSet<PostTag> PostTags { get; set; }
        public virtual DbSet<PostImage> PostImages { get; set; }

        public WoodyDbContext(DbContextOptions<WoodyDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Username).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Community>(e =>
            {
                e.HasIndex(c => c.Slug).IsUnique();
                e.HasOne(c => c.Owner)
                    .WithMany(u => u.OwnedCommunities)
                    .HasForeignKey(c => c.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommunityMembership>(e =>
            {
                e.HasIndex(m => new { m.UserId, m.CommunityId }).IsUnique();
            });

            modelBuilder.Entity<JoinRequest>(e =>
            {
                e.HasIndex(j => new { j.CommunityId, j.UserId, j.Status });
            });

            modelBuilder.Entity<Post>(e =>
            {
                e.ToTable(
                    "posts",
                    t => t.HasCheckConstraint(
                        "ck_posts_publication_context_community",
                        "(publication_context = 2 AND community_id IS NOT NULL) OR (publication_context = 1 AND community_id IS NULL)"));

                e.HasOne(p => p.Community)
                    .WithMany(c => c.Posts)
                    .HasForeignKey(p => p.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(p => new { p.PublicationContext, p.UserId });
            });

            modelBuilder.Entity<PostImage>(e =>
            {
                e.HasOne(i => i.Post)
                    .WithMany(p => p.Images)
                    .HasForeignKey(i => i.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.FollowingUserId, f.FollowedUserId });

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowingUser)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowedUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Like>()
                .HasIndex(l => new { l.UserId, l.TargetType, l.TargetId })
                .IsUnique();

            modelBuilder.Entity<ContentReport>(e =>
            {
                e.HasOne(r => r.Reporter)
                    .WithMany(u => u.ContentReports)
                    .HasForeignKey(r => r.ReporterUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Post)
                    .WithMany()
                    .HasForeignKey(r => r.PostId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Comment)
                    .WithMany()
                    .HasForeignKey(r => r.CommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
