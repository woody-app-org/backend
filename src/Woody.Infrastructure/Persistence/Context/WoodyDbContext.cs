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
        public virtual DbSet<Topic> Topics { get; set; }
        public virtual DbSet<UserTopic> UserTopics { get; set; }
        public virtual DbSet<PostTopic> PostTopics { get; set; }

        public WoodyDbContext(DbContextOptions<WoodyDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<PostTopic>()
                .HasKey(pt => new { pt.PostId, pt.TopicId });

            modelBuilder.Entity<PostTopic>()
                .HasOne(pt => pt.Post)
                .WithMany(p => p.PostTopics)
                .HasForeignKey(pt => pt.PostId);

            modelBuilder.Entity<PostTopic>()
                .HasOne(pt => pt.Topic)
                .WithMany(t => t.PostTopics)
                .HasForeignKey(pt => pt.TopicId);

            modelBuilder.Entity<UserTopic>()
                .HasKey(ut => new { ut.UserId, ut.TopicId });

            modelBuilder.Entity<UserTopic>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTopics)
                .HasForeignKey(ut => ut.UserId);

            modelBuilder.Entity<UserTopic>()
                .HasOne(ut => ut.Topic)
                .WithMany(t => t.UserTopics)
                .HasForeignKey(ut => ut.TopicId);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}