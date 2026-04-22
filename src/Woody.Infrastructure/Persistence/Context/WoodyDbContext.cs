using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

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
        public virtual DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
        public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }
        public virtual DbSet<BillingWebhookReceipt> BillingWebhookReceipts { get; set; }
        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<MessageAttachment> MessageAttachments { get; set; }

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

            modelBuilder.Entity<UserSubscription>(e =>
            {
                e.ToTable("subscriptions");
                e.HasKey(x => x.UserId);
                e.HasIndex(x => x.ProviderSubscriptionId)
                    .IsUnique()
                    .HasFilter("provider_subscription_id IS NOT NULL");
                e.HasOne(x => x.User)
                    .WithOne(u => u.Subscription)
                    .HasForeignKey<UserSubscription>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BillingWebhookReceipt>(e =>
            {
                e.ToTable("billing_webhook_receipts");
                e.HasKey(x => x.EventId);
            });

            modelBuilder.Entity<EmailVerificationCode>(e =>
            {
                e.HasIndex(x => new { x.Email, x.CreatedAt });
                e.HasOne(x => x.User)
                    .WithMany(u => u.EmailVerificationCodes)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
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
                e.HasIndex(p => new { p.UserId, p.PinnedOnProfileAt });
            });

            modelBuilder.Entity<PostImage>(e =>
            {
                e.HasOne(i => i.Post)
                    .WithMany(p => p.Images)
                    .HasForeignKey(i => i.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Chave composta = unicidade (following_user_id, followed_user_id); equivale a UNIQUE + PK no SQL Server.
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

            modelBuilder.Entity<Comment>(e =>
            {
                e.HasIndex(c => c.PostId)
                    .IsUnique()
                    .HasFilter("pinned_on_post_at IS NOT NULL");

                e.HasOne(c => c.ParentComment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

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

            modelBuilder.Entity<Conversation>(e =>
            {
                e.ToTable(
                    "conversations",
                    t =>
                    {
                        t.HasCheckConstraint("ck_conversations_ordered_pair", "user_low_id < user_high_id");
                        t.HasCheckConstraint(
                            "ck_conversations_pending_has_initiator",
                            "(status <> "
                            + (int)ConversationStatus.Pending
                            + ") OR (initiator_user_id IS NOT NULL AND (initiator_user_id = user_low_id OR initiator_user_id = user_high_id))");
                    });

                e.HasIndex(c => new { c.UserLowId, c.UserHighId }).IsUnique();

                e.HasOne(c => c.UserLow)
                    .WithMany(u => u.ConversationsAsUserLow)
                    .HasForeignKey(c => c.UserLowId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.UserHigh)
                    .WithMany(u => u.ConversationsAsUserHigh)
                    .HasForeignKey(c => c.UserHighId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.Initiator)
                    .WithMany(u => u.InitiatedConversations)
                    .HasForeignKey(c => c.InitiatorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConversationParticipant>(e =>
            {
                e.HasKey(p => new { p.ConversationId, p.UserId });
                e.HasIndex(p => p.UserId);

                e.HasOne(p => p.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(p => p.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.User)
                    .WithMany(u => u.ConversationParticipations)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Message>(e =>
            {
                e.HasIndex(m => new { m.ConversationId, m.CreatedAt });

                e.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MessageAttachment>(e =>
            {
                e.HasOne(a => a.Message)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(a => a.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
