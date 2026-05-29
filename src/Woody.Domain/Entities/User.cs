using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;

        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Pronouns { get; set; }
        public string? ProfilePic { get; set; }
        public string? BannerPic { get; set; }
        /// <summary>Título profissional / ocupação exibido no perfil (independente de <see cref="Role"/> na plataforma).</summary>
        public string? Profession { get; set; }
        public string? Location { get; set; }
        public string? Cpf { get; set; }
        public DateOnly? BirthDate { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }

        /// <summary>Cache desnormalizado do status de verificação de identidade. Atualizado em sincronia com <see cref="IdentityVerification"/>.</summary>
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.PendingDocument;

        public IdentityVerification? IdentityVerification { get; set; }

        public UserSubscription? Subscription { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int? InviteId { get; set; }
        public BetaInvite? Invite { get; set; }

        /// <summary>Quem pode enviar sinais de perfil para esta utilizadora.</summary>
        public ProfileSignalsIncomingPreference ProfileSignalsIncomingPreference { get; set; }

        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();

        public ICollection<Follow> Following { get; set; } = new List<Follow>();
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();

        public ICollection<Community> OwnedCommunities { get; set; } = new List<Community>();
        public ICollection<CommunityMembership> CommunityMemberships { get; set; } = new List<CommunityMembership>();
        public ICollection<JoinRequest> JoinRequests { get; set; } = new List<JoinRequest>();
        public ICollection<UserSocialLink> SocialLinks { get; set; } = new List<UserSocialLink>();
        public ICollection<UserInterest> Interests { get; set; } = new List<UserInterest>();
        public ICollection<ContentReport> ContentReports { get; set; } = new List<ContentReport>();
        public ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; } = new List<EmailVerificationCode>();
        public ICollection<RefreshTokenSession> RefreshTokenSessions { get; set; } = new List<RefreshTokenSession>();
        public ICollection<PasswordResetSession> PasswordResetSessions { get; set; } = new List<PasswordResetSession>();

        public ICollection<Conversation> ConversationsAsUserLow { get; set; } = new List<Conversation>();
        public ICollection<Conversation> ConversationsAsUserHigh { get; set; } = new List<Conversation>();
        public ICollection<Conversation> InitiatedConversations { get; set; } = new List<Conversation>();
        public ICollection<ConversationParticipant> ConversationParticipations { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<ProfileSignal> SentProfileSignals { get; set; } = new List<ProfileSignal>();
        public ICollection<ProfileSignal> ReceivedProfileSignals { get; set; } = new List<ProfileSignal>();
        public ICollection<Story> Stories { get; set; } = new List<Story>();
        public ICollection<StoryView> StoryViews { get; set; } = new List<StoryView>();
    }
}
