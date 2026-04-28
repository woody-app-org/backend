namespace Woody.Application.DTOs.Api;

public class SendProfileSignalRequestDto
{
    public int ReceiverUserId { get; set; }
    public int RecipientUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class ProfileSignalResponseDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? ReadAt { get; set; }
    public string? ArchivedAt { get; set; }
    public string? DismissedAt { get; set; }
    public UserPublicDto Sender { get; set; } = null!;
    public UserPublicDto Receiver { get; set; } = null!;
    public UserPublicDto Recipient { get; set; } = null!;
}

public class ProfileSignalStatusResponseDto
{
    public int ReceiverUserId { get; set; }
    public int RecipientUserId { get; set; }
    public bool CanSend { get; set; }
    public string? LastSentAt { get; set; }
    public string? NextAllowedAt { get; set; }
}
