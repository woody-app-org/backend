using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities
{
    public class EmailVerificationCode
    {
        public int Id { get; set; }
        public VerificationCodePurpose Purpose { get; set; } = VerificationCodePurpose.EmailConfirmation;
        public string Email { get; set; } = null!;
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string CodeHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public DateTime? InvalidatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
