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
    public string Emoji { get; set; } = string.Empty;
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

    /// <summary>
    /// Quando <see cref="CanSend"/> é falso: <c>cooldown</c>, <c>blocked</c>, <c>receiver_unavailable</c>, <c>social_mismatch</c>.
    /// </summary>
    public string? RestrictionCode { get; set; }

    /// <summary>
    /// Falso se bloqueio ou preferência da destinatária impedem o envio (independentemente do tipo/cooldown).
    /// </summary>
    public bool SenderEligible { get; set; } = true;

    /// <summary>Código quando <see cref="SenderEligible"/> é falso (igual a <see cref="RestrictionCode"/> nesses casos).</summary>
    public string? EligibilityRestrictionCode { get; set; }
}

public class UpdateProfileSignalsIncomingPreferenceDto
{
    public string IncomingPreference { get; set; } = "all";
}

public class ProfileSignalsIncomingPreferenceResponseDto
{
    public string IncomingPreference { get; set; } = "all";
}

/// <summary>Sinais em estado &quot;Sent&quot; (ainda não lidos pela destinatária).</summary>
public class ProfileSignalsUnreadCountDto
{
    public int UnreadCount { get; set; }
}
