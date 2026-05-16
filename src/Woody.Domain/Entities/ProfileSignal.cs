using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>Sinal privado enviado de uma utilizadora para outra a partir do perfil.</summary>
public class ProfileSignal
{
    public int Id { get; set; }

    public int SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    public int ReceiverUserId { get; set; }
    public User ReceiverUser { get; set; } = null!;

    public ProfileSignalType Type { get; set; }
    public string? Message { get; set; }
    public ProfileSignalStatus Status { get; set; } = ProfileSignalStatus.Sent;

    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? DismissedAt { get; set; }
}
