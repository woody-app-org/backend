using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public enum ProfileSignalOperationOutcome
{
    Success,
    InvalidReceiver,
    InvalidType,
    SelfSignal,
    ReceiverNotFound,
    CooldownActive,
    NotFound,
    Forbidden,
    /// <summary>Bloqueio entre utilizadoras (quando existir infraestrutura).</summary>
    InteractionBlocked,
    /// <summary>Destinatária não aceita sinais de ninguém.</summary>
    ReceiverDeclinesSignals,
    /// <summary>Destinatária só aceita subconjunto social e remetente não se enquadra.</summary>
    SenderNotEligibleBySocialRules
}

public sealed record ProfileSignalsIncomingPreferenceUpdateResult(bool Ok, string? Error);

public sealed record ProfileSignalCommandResult(
    ProfileSignalOperationOutcome Outcome,
    ProfileSignalResponseDto? Signal = null,
    string? Error = null,
    DateTime? NextAllowedAt = null);

public interface IProfileSignalService
{
    Task<ProfileSignalCommandResult> SendAsync(
        int senderUserId,
        int receiverUserId,
        string type,
        string? message,
        CancellationToken cancellationToken = default);

    Task<PaginatedResponseDto<ProfileSignalResponseDto>> ListReceivedAsync(
        int receiverUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaginatedResponseDto<ProfileSignalResponseDto>> ListSentAsync(
        int senderUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProfileSignalStatusResponseDto> GetSendStatusAsync(
        int senderUserId,
        int receiverUserId,
        string type,
        CancellationToken cancellationToken = default);

    Task<ProfileSignalsUnreadCountDto> GetUnreadReceivedCountAsync(
        int receiverUserId,
        CancellationToken cancellationToken = default);

    Task<ProfileSignalsIncomingPreferenceResponseDto> GetMyIncomingPreferenceAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ProfileSignalsIncomingPreferenceUpdateResult> UpdateMyIncomingPreferenceAsync(
        int userId,
        string rawPreference,
        CancellationToken cancellationToken = default);

    Task<ProfileSignalCommandResult> MarkReadAsync(int actorUserId, int signalId, CancellationToken cancellationToken = default);
    Task<ProfileSignalCommandResult> ArchiveAsync(int actorUserId, int signalId, CancellationToken cancellationToken = default);
    Task<ProfileSignalCommandResult> DismissAsync(int actorUserId, int signalId, CancellationToken cancellationToken = default);
}
