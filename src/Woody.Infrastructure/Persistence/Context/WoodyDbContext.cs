using Microsoft.EntityFrameworkCore;
using Woody.Application.Validation;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;
using Woody.Domain.Stories;

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
        public virtual DbSet<MediaAttachment> MediaAttachments { get; set; }
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
        public virtual DbSet<ProfileSignal> ProfileSignals { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<BetaInvite> BetaInvites { get; set; }
        public virtual DbSet<IdentityVerification> IdentityVerifications { get; set; }
        public virtual DbSet<PreLaunchSignup> PreLaunchSignups { get; set; }
        public virtual DbSet<Story> Stories { get; set; }
        public virtual DbSet<StoryView> StoryViews { get; set; }

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
                e.Property(u => u.VerificationStatus)
                    .HasConversion<string>()
                    .HasDefaultValue(VerificationStatus.PendingDocument);
                e.Property(u => u.Profession).HasMaxLength(60);

                e.HasOne(u => u.Invite)
                    .WithMany(i => i.Users)
                    .HasForeignKey(u => u.InviteId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<IdentityVerification>(e =>
            {
                e.ToTable("identity_verifications");
                e.HasIndex(v => v.UserId).IsUnique();
                e.Property(v => v.Status)
                    .HasConversion<string>()
                    .HasDefaultValue(VerificationStatus.PendingDocument);
                e.Property(v => v.DocumentStorageKey).HasMaxLength(500);
                e.Property(v => v.RejectionReason).HasMaxLength(1000);
                e.Property(v => v.DecisionLog).HasColumnType("text");

                e.HasOne(v => v.User)
                    .WithOne(u => u.IdentityVerification)
                    .HasForeignKey<IdentityVerification>(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(v => v.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(v => v.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<BetaInvite>(e =>
            {
                e.ToTable("beta_invites");
                e.HasIndex(i => i.Code).IsUnique();
                e.Property(i => i.Code).HasMaxLength(128);
                e.Property(i => i.Label).HasMaxLength(256);
                e.Property(i => i.CreatedBy).HasMaxLength(256);
                e.HasCheckConstraint("ck_beta_invites_max_uses_positive", "max_uses > 0");
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
                e.Property(j => j.RejectionReason).HasMaxLength(InputValidationLimits.JoinRequestRejectionReasonMaxLength);
                e.HasOne(j => j.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(j => j.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(j => new { j.CommunityId, j.UserId })
                    .IsUnique()
                    .HasDatabaseName("ux_join_requests_community_user_pending")
                    .HasFilter("status = 'pending'");
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

            modelBuilder.Entity<MediaAttachment>(e =>
            {
                e.ToTable(
                    "media_attachments",
                    t => t.HasCheckConstraint(
                        "ck_media_attachments_owner_xor",
                        "(owner_type = 1 AND post_id IS NOT NULL AND post_id = owner_id AND message_id IS NULL) "
                        + "OR (owner_type = 2 AND message_id IS NOT NULL AND message_id = owner_id AND post_id IS NULL)"));

                e.HasIndex(x => new { x.OwnerType, x.OwnerId, x.DisplayOrder })
                    .HasDatabaseName("ix_media_attachments_owner_type_owner_id_display_order");

                e.HasOne(x => x.Post)
                    .WithMany(p => p.MediaAttachments)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Message)
                    .WithMany(m => m.MediaAttachments)
                    .HasForeignKey(x => x.MessageId)
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
                e.Property(c => c.GifUrl).HasMaxLength(PublicImageUrlPolicy.MaxUrlLength);
                e.Property(c => c.GifThumbnailUrl).HasMaxLength(PublicImageUrlPolicy.MaxUrlLength);
                e.Property(c => c.GifProvider).HasMaxLength(InputValidationLimits.CommentGifProviderMaxLength);
                e.Property(c => c.GifExternalId).HasMaxLength(InputValidationLimits.CommentGifExternalIdMaxLength);
                e.Property(c => c.GifTitle).HasMaxLength(InputValidationLimits.CommentGifTitleMaxLength);

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

            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("notifications");
                e.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
                e.HasIndex(n => new { n.RecipientUserId, n.ReadAt });
                e.HasIndex(n => n.Type);
                e.HasIndex(n => new { n.TargetKind, n.TargetId });
                e.Property(n => n.Type).HasConversion<int>();
                e.Property(n => n.TargetKind).HasConversion<int>();
                e.Property(n => n.Title).HasMaxLength(256);
                e.Property(n => n.Message).HasMaxLength(1000);
                e.Property(n => n.MetadataJson).HasMaxLength(4000).IsRequired();

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

            modelBuilder.Entity<Story>(e =>
            {
                e.ToTable("stories");
                e.Property(s => s.MediaType).HasConversion<int>();
                e.Property(s => s.Visibility).HasConversion<int>().HasDefaultValue(StoryVisibility.Public);
                e.Property(s => s.MediaUrl).HasMaxLength(2048);
                e.Property(s => s.ThumbnailUrl).HasMaxLength(2048);
                e.Property(s => s.StorageKey).HasMaxLength(512);
                e.Property(s => s.Text).HasMaxLength(StoryPolicies.MaxTextLength);
                e.Property(s => s.BackgroundColor).HasMaxLength(StoryPolicies.MaxBackgroundColorLength);
                e.Property(s => s.MusicProvider).HasMaxLength(32);
                e.Property(s => s.MusicTrackId).HasMaxLength(128);
                e.Property(s => s.MusicTitle).HasMaxLength(256);
                e.Property(s => s.MusicArtist).HasMaxLength(256);
                e.Property(s => s.MusicPreviewUrl).HasMaxLength(2048);

                e.HasIndex(s => s.ExpiresAt);
                e.HasIndex(s => new { s.AuthorUserId, s.ExpiresAt })
                    .HasFilter("deleted_at IS NULL")
                    .HasDatabaseName("ix_stories_author_expires_not_deleted");

                e.HasOne(s => s.Author)
                    .WithMany(u => u.Stories)
                    .HasForeignKey(s => s.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StoryView>(e =>
            {
                e.ToTable("story_views");
                e.HasIndex(v => new { v.StoryId, v.ViewerUserId })
                    .IsUnique()
                    .HasDatabaseName("ux_story_views_story_id_viewer_user_id");
                e.HasIndex(v => v.ViewedAt);

                e.HasOne(v => v.Story)
                    .WithMany(s => s.Views)
                    .HasForeignKey(v => v.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(v => v.Viewer)
                    .WithMany(u => u.StoryViews)
                    .HasForeignKey(v => v.ViewerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PreLaunchSignup>(e =>
            {
                e.ToTable("pre_launch_signups");
                e.HasIndex(s => new { s.NormalizedSocialNetwork, s.NormalizedSocialUsername })
                    .IsUnique()
                    .HasDatabaseName("ux_pre_launch_signups_network_username");
                e.Property(s => s.Name).HasMaxLength(120);
                e.Property(s => s.SocialNetwork).HasMaxLength(32);
                e.Property(s => s.SocialUsername).HasMaxLength(80);
                e.Property(s => s.NormalizedSocialNetwork).HasMaxLength(32);
                e.Property(s => s.NormalizedSocialUsername).HasMaxLength(80);
                e.Property(s => s.IpHash).HasMaxLength(64);
                e.Property(s => s.UserAgentHash).HasMaxLength(64);
                e.Property(s => s.Source).HasMaxLength(128);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
