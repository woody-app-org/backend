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
        public virtual DbSet<CommunitySubscription> CommunitySubscriptions { get; set; }
        public virtual DbSet<CommunityDailyRollup> CommunityDailyRollups { get; set; }
        public virtual DbSet<CommunityPostBoost> CommunityPostBoosts { get; set; }
        public virtual DbSet<BillingWebhookReceipt> BillingWebhookReceipts { get; set; }
        public virtual DbSet<BillingCheckoutAttempt> BillingCheckoutAttempts { get; set; }
        public virtual DbSet<RefreshTokenSession> RefreshTokenSessions { get; set; }
        public virtual DbSet<LoginLockout> LoginLockouts { get; set; }
        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<MessageAttachment> MessageAttachments { get; set; }
        public virtual DbSet<ProfileSignal> ProfileSignals { get; set; }
        public virtual DbSet<UserNotification> UserNotifications { get; set; }

        public WoodyDbContext(DbContextOptions<WoodyDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Username).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.ProfileSignalsIncomingPreference)
                    .HasConversion<int>()
                    .HasDefaultValue(ProfileSignalsIncomingPreference.All);
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

            modelBuilder.Entity<BillingCheckoutAttempt>(e =>
            {
                e.ToTable("billing_checkout_attempts");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IdempotencyKey).IsUnique();
                e.HasIndex(x => new { x.UserId, x.SubjectKind, x.PlanCode, x.CommunityId, x.Status });
                e.Property(x => x.IdempotencyKey).HasMaxLength(200);
                e.Property(x => x.PlanCode).HasMaxLength(64);
                e.Property(x => x.StripeSessionId).HasMaxLength(128);
                e.Property(x => x.StripeCustomerId).HasMaxLength(128);
            });

            modelBuilder.Entity<EmailVerificationCode>(e =>
            {
                e.HasIndex(x => new { x.Email, x.CreatedAt });
                e.HasOne(x => x.User)
                    .WithMany(u => u.EmailVerificationCodes)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RefreshTokenSession>(e =>
            {
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => new { x.UserId, x.ExpiresAt });
                e.HasOne(x => x.User)
                    .WithMany(u => u.RefreshTokenSessions)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LoginLockout>(e =>
            {
                e.HasKey(x => x.NormalizedLogin);
                e.HasIndex(x => x.LockoutEndAt);
            });

            modelBuilder.Entity<Community>(e =>
            {
                e.HasIndex(c => c.Slug).IsUnique();
                e.HasOne(c => c.Owner)
                    .WithMany(u => u.OwnedCommunities)
                    .HasForeignKey(c => c.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommunitySubscription>(e =>
            {
                e.ToTable("community_subscriptions");
                e.HasKey(x => x.CommunityId);
                e.HasIndex(x => x.ProviderSubscriptionId)
                    .IsUnique()
                    .HasFilter("provider_subscription_id IS NOT NULL");
                e.HasOne(x => x.Community)
                    .WithOne(c => c.Subscription)
                    .HasForeignKey<CommunitySubscription>(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CommunityDailyRollup>(e =>
            {
                e.HasKey(x => new { x.CommunityId, x.DayUtc });
                e.HasIndex(x => x.CommunityId);
                e.HasOne(x => x.Community)
                    .WithMany(c => c.DailyRollups)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CommunityPostBoost>(e =>
            {
                e.HasIndex(x => x.CommunityId);
                e.HasIndex(x => x.PostId);
                e.HasIndex(x => new { x.CommunityId, x.EndsAtUtc });
                e.HasOne(x => x.Post)
                    .WithMany(p => p.CommunityPostBoosts)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Community)
                    .WithMany(c => c.PostBoosts)
                    .HasForeignKey(x => x.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            modelBuilder.Entity<UserNotification>(e =>
            {
                e.ToTable("user_notifications");
                e.HasIndex(n => new { n.RecipientUserId, n.CreatedAtUtc });
                e.HasIndex(n => new { n.RecipientUserId, n.ReadAtUtc });
                e.Property(n => n.Type).HasMaxLength(48).IsRequired();
                e.Property(n => n.PayloadJson).HasMaxLength(4000).IsRequired();

                e.HasOne(n => n.RecipientUser)
                    .WithMany()
                    .HasForeignKey(n => n.RecipientUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(n => n.ActorUser)
                    .WithMany()
                    .HasForeignKey(n => n.ActorUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProfileSignal>(e =>
            {
                e.ToTable("profile_signals");
                e.HasIndex(s => s.ReceiverUserId);
                e.HasIndex(s => s.SenderUserId);
                e.HasIndex(s => s.CreatedAt);
                e.HasIndex(s => new { s.SenderUserId, s.ReceiverUserId, s.Type, s.CreatedAt });
                e.HasIndex(s => new { s.ReceiverUserId, s.Status, s.CreatedAt });
                e.HasIndex(s => new { s.SenderUserId, s.CreatedAt });
                e.Property(s => s.Message).HasMaxLength(160);

                e.HasOne(s => s.SenderUser)
                    .WithMany(u => u.SentProfileSignals)
                    .HasForeignKey(s => s.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.ReceiverUser)
                    .WithMany(u => u.ReceivedProfileSignals)
                    .HasForeignKey(s => s.ReceiverUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
