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
    Forbidden
}

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

    Task<ProfileSignalCommandResult> MarkReadAsync(int actorUserId, int signalId, CancellationToken cancellationToken = default);
    Task<ProfileSignalCommandResult> ArchiveAsync(int actorUserId, int signalId, CancellationToken cancellationToken = default);
    Task<ProfileSignalCommandResult> DismissAsync(int actorUserId, int signalId, CancellationToken cancellationToken = default);
}
