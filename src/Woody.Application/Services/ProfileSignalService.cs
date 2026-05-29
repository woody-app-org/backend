using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class ProfileSignalService : IProfileSignalService
{
    private static readonly TimeSpan SendCooldown = TimeSpan.FromHours(24);
    private const int MaxMessageLength = 160;

    private const string MsgCooldown = "Você já enviou esse sinal recentemente.";
    private const string MsgReceiverUnavailable = "Essa pessoa não está recebendo sinais no momento.";
    private const string MsgInteractionBlocked = "Não foi possível enviar este sinal.";

    private readonly IProfileSignalRepository _signals;
    private readonly IUserRepository _users;
    private readonly IFollowRepository _follows;
    private readonly IProfileSignalSocialGate _socialGate;
    private readonly INotificationService _notificationService;

    public ProfileSignalService(
        IProfileSignalRepository signals,
        IUserRepository users,
        IFollowRepository follows,
        IProfileSignalSocialGate socialGate,
        INotificationService notificationService)
    {
        _signals = signals;
        _users = users;
        _follows = follows;
        _socialGate = socialGate;
        _notificationService = notificationService;
    }

    public async Task<ProfileSignalCommandResult> SendAsync(
        int senderUserId,
        int receiverUserId,
        string type,
        string? message,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSignalType(type, out var signalType))
            return Failure(ProfileSignalOperationOutcome.InvalidType, "Tipo de sinal inválido.");

        if (receiverUserId <= 0)
            return Failure(ProfileSignalOperationOutcome.InvalidReceiver, "Destinatária inválida.");

        if (receiverUserId == senderUserId)
            return Failure(ProfileSignalOperationOutcome.SelfSignal, "Não podes enviar sinal para ti própria.");

        var receiver = await _users.GetByIdNoTrackingAsync(receiverUserId, cancellationToken);
        if (receiver == null)
            return Failure(ProfileSignalOperationOutcome.ReceiverNotFound, "Utilizadora não encontrada.");

        var gateOutcome = await EvaluateSendGateAsync(senderUserId, receiverUserId, receiver, cancellationToken);
        if (gateOutcome != null)
            return Failure(gateOutcome.Value, MessageForGateOutcome(gateOutcome.Value));

        var now = DateTime.UtcNow;
        var since = now.Subtract(SendCooldown);
        if (await _signals.HasSentTypeSinceAsync(senderUserId, receiverUserId, signalType, since, cancellationToken))
        {
            var latest = await _signals.GetLatestOfTypeBetweenAsync(senderUserId, receiverUserId, signalType, cancellationToken);
            var nextAllowedAt = latest?.CreatedAt.Add(SendCooldown);
            return Failure(
                ProfileSignalOperationOutcome.CooldownActive,
                MsgCooldown,
                nextAllowedAt);
        }

        var signal = new ProfileSignal
        {
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            Type = signalType,
            Message = NormalizeMessage(message),
            Status = ProfileSignalStatus.Sent,
            CreatedAt = now
        };

        _signals.Add(signal);
        await _signals.SaveChangesAsync(cancellationToken);

        var created = await _signals.GetByIdWithUsersAsync(signal.Id, cancellationToken) ?? signal;
        await _notificationService.NotifyProfileSignalAsync(
            senderUserId,
            receiverUserId,
            created.Id,
            EntityMappers.ToProfileSignalTypeApi(signalType),
            cancellationToken);
        return new ProfileSignalCommandResult(ProfileSignalOperationOutcome.Success, EntityMappers.ToProfileSignalDto(created));
    }

    public async Task<PaginatedResponseDto<ProfileSignalResponseDto>> ListReceivedAsync(
        int receiverUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _signals.ListReceivedInboxPagedAsync(receiverUserId, page, pageSize, cancellationToken);
        return ToPage(items, total, page, pageSize);
    }

    public async Task<PaginatedResponseDto<ProfileSignalResponseDto>> ListSentAsync(
        int senderUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _signals.ListSentPagedAsync(senderUserId, page, pageSize, cancellationToken);
        return ToPage(items, total, page, pageSize);
    }

    public async Task<ProfileSignalStatusResponseDto> GetSendStatusAsync(
        int senderUserId,
        int receiverUserId,
        string type,
        CancellationToken cancellationToken = default)
    {
        var dto = new ProfileSignalStatusResponseDto
        {
            ReceiverUserId = receiverUserId,
            RecipientUserId = receiverUserId,
            CanSend = false
        };

        if (!TryParseSignalType(type, out var signalType))
        {
            dto.SenderEligible = false;
            return dto;
        }

        if (receiverUserId <= 0 || receiverUserId == senderUserId)
        {
            dto.SenderEligible = false;
            return dto;
        }

        var receiver = await _users.GetByIdNoTrackingAsync(receiverUserId, cancellationToken);
        if (receiver == null)
        {
            dto.SenderEligible = false;
            return dto;
        }

        var gateOutcome = await EvaluateSendGateAsync(senderUserId, receiverUserId, receiver, cancellationToken);
        if (gateOutcome != null)
        {
            var code = MapGateOutcomeToRestrictionCode(gateOutcome.Value);
            dto.CanSend = false;
            dto.SenderEligible = false;
            dto.EligibilityRestrictionCode = code;
            dto.RestrictionCode = code;
            dto.LastSentAt = null;
            dto.NextAllowedAt = null;
            return dto;
        }

        dto.SenderEligible = true;
        dto.EligibilityRestrictionCode = null;

        var latest = await _signals.GetLatestOfTypeBetweenAsync(senderUserId, receiverUserId, signalType, cancellationToken);
        var nextAllowedAt = latest?.CreatedAt.Add(SendCooldown);
        var canSend = nextAllowedAt == null || nextAllowedAt <= DateTime.UtcNow;

        dto.CanSend = canSend;
        dto.RestrictionCode = canSend ? null : "cooldown";
        dto.LastSentAt = latest != null ? EntityMappers.Iso(latest.CreatedAt) : null;
        dto.NextAllowedAt = !canSend && nextAllowedAt.HasValue ? EntityMappers.Iso(nextAllowedAt.Value) : null;
        return dto;
    }

    public async Task<ProfileSignalsUnreadCountDto> GetUnreadReceivedCountAsync(
        int receiverUserId,
        CancellationToken cancellationToken = default)
    {
        var count = await _signals.CountUnreadReceivedAsync(receiverUserId, cancellationToken);
        return new ProfileSignalsUnreadCountDto { UnreadCount = count };
    }

    public async Task<ProfileSignalsIncomingPreferenceResponseDto> GetMyIncomingPreferenceAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdNoTrackingAsync(userId, cancellationToken);
        if (user == null)
            return new ProfileSignalsIncomingPreferenceResponseDto { IncomingPreference = "all" };

        return new ProfileSignalsIncomingPreferenceResponseDto
        {
            IncomingPreference = ProfileSignalSendRules.ToApiString(user.ProfileSignalsIncomingPreference)
        };
    }

    public async Task<ProfileSignalsIncomingPreferenceUpdateResult> UpdateMyIncomingPreferenceAsync(
        int userId,
        string rawPreference,
        CancellationToken cancellationToken = default)
    {
        if (!ProfileSignalSendRules.TryParseIncomingPreference(rawPreference, out var pref))
            return new ProfileSignalsIncomingPreferenceUpdateResult(false, "Preferência inválida.");

        var user = await _users.GetByIdTrackedAsync(userId, cancellationToken);
        if (user == null)
            return new ProfileSignalsIncomingPreferenceUpdateResult(false, "Utilizadora não encontrada.");

        user.ProfileSignalsIncomingPreference = pref;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
        return new ProfileSignalsIncomingPreferenceUpdateResult(true, null);
    }

    public Task<ProfileSignalCommandResult> MarkReadAsync(
        int actorUserId,
        int signalId,
        CancellationToken cancellationToken = default) =>
        MutateReceiverSignalAsync(actorUserId, signalId, ProfileSignalStatus.Read, cancellationToken);

    public Task<ProfileSignalCommandResult> ArchiveAsync(
        int actorUserId,
        int signalId,
        CancellationToken cancellationToken = default) =>
        MutateReceiverSignalAsync(actorUserId, signalId, ProfileSignalStatus.Archived, cancellationToken);

    public Task<ProfileSignalCommandResult> DismissAsync(
        int actorUserId,
        int signalId,
        CancellationToken cancellationToken = default) =>
        MutateReceiverSignalAsync(actorUserId, signalId, ProfileSignalStatus.Dismissed, cancellationToken);

    private async Task<ProfileSignalOperationOutcome?> EvaluateSendGateAsync(
        int senderUserId,
        int receiverUserId,
        User receiver,
        CancellationToken cancellationToken)
    {
        if (await _socialGate.AreUsersBlockedEitherWayAsync(senderUserId, receiverUserId, cancellationToken))
            return ProfileSignalOperationOutcome.InteractionBlocked;

        var senderFollowsReceiver = await _follows.ExistsAsync(senderUserId, receiverUserId, cancellationToken);
        var receiverFollowsSender = await _follows.ExistsAsync(receiverUserId, senderUserId, cancellationToken);

        if (ProfileSignalSendRules.MeetsIncomingPreference(
                receiver.ProfileSignalsIncomingPreference,
                senderFollowsReceiver,
                receiverFollowsSender))
            return null;

        return receiver.ProfileSignalsIncomingPreference == ProfileSignalsIncomingPreference.Nobody
            ? ProfileSignalOperationOutcome.ReceiverDeclinesSignals
            : ProfileSignalOperationOutcome.SenderNotEligibleBySocialRules;
    }

    private static string MessageForGateOutcome(ProfileSignalOperationOutcome outcome) =>
        outcome switch
        {
            ProfileSignalOperationOutcome.InteractionBlocked => MsgInteractionBlocked,
            ProfileSignalOperationOutcome.ReceiverDeclinesSignals => MsgReceiverUnavailable,
            ProfileSignalOperationOutcome.SenderNotEligibleBySocialRules => MsgReceiverUnavailable,
            _ => MsgReceiverUnavailable
        };

    private static string? MapGateOutcomeToRestrictionCode(ProfileSignalOperationOutcome outcome) =>
        outcome switch
        {
            ProfileSignalOperationOutcome.InteractionBlocked => "receiver_unavailable",
            ProfileSignalOperationOutcome.ReceiverDeclinesSignals => "receiver_unavailable",
            ProfileSignalOperationOutcome.SenderNotEligibleBySocialRules => "social_mismatch",
            _ => null
        };

    private async Task<ProfileSignalCommandResult> MutateReceiverSignalAsync(
        int actorUserId,
        int signalId,
        ProfileSignalStatus nextStatus,
        CancellationToken cancellationToken)
    {
        var signal = await _signals.GetByIdWithUsersAsync(signalId, cancellationToken);
        if (signal == null)
            return Failure(ProfileSignalOperationOutcome.NotFound, "Sinal não encontrado.");

        if (signal.ReceiverUserId != actorUserId)
            return Failure(ProfileSignalOperationOutcome.Forbidden, "Só a destinatária pode alterar este sinal.");

        var now = DateTime.UtcNow;
        switch (nextStatus)
        {
            case ProfileSignalStatus.Read:
                if (signal.Status == ProfileSignalStatus.Sent)
                {
                    signal.Status = ProfileSignalStatus.Read;
                    signal.ReadAt = now;
                }
                break;
            case ProfileSignalStatus.Archived:
                signal.Status = ProfileSignalStatus.Archived;
                signal.ArchivedAt ??= now;
                break;
            case ProfileSignalStatus.Dismissed:
                signal.Status = ProfileSignalStatus.Dismissed;
                signal.DismissedAt ??= now;
                break;
        }

        await _signals.SaveChangesAsync(cancellationToken);
        return new ProfileSignalCommandResult(ProfileSignalOperationOutcome.Success, EntityMappers.ToProfileSignalDto(signal));
    }

    private static string? NormalizeMessage(string? message)
    {
        var trimmed = message?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length <= MaxMessageLength ? trimmed : trimmed[..MaxMessageLength];
    }

    private static ProfileSignalCommandResult Failure(
        ProfileSignalOperationOutcome outcome,
        string error,
        DateTime? nextAllowedAt = null) =>
        new(outcome, Error: error, NextAllowedAt: nextAllowedAt);

    private static bool TryParseSignalType(string? raw, out ProfileSignalType type)
    {
        type = default;
        return raw?.Trim().ToLowerInvariant() switch
        {
            "te_notei" => Set(ProfileSignalType.TeNotei, out type),
            "olhadinha" => Set(ProfileSignalType.Olhadinha, out type),
            "conhecer_mais" => Set(ProfileSignalType.ConhecerMais, out type),
            "quero_conversar" => Set(ProfileSignalType.QueroConversar, out type),
            "crush_fofo" => Set(ProfileSignalType.CrushFofo, out type),
            "atracao" => Set(ProfileSignalType.Atracao, out type),
            "sinal_verde" => Set(ProfileSignalType.SinalVerde, out type),
            "cheguei" => Set(ProfileSignalType.Cheguei, out type),
            _ => false
        };
    }

    private static bool Set(ProfileSignalType value, out ProfileSignalType type)
    {
        type = value;
        return true;
    }

    private static PaginatedResponseDto<ProfileSignalResponseDto> ToPage(
        List<ProfileSignal> items,
        int total,
        int page,
        int pageSize) => new()
    {
        Items = items.Select(EntityMappers.ToProfileSignalDto).ToList(),
        Page = page,
        PageSize = pageSize,
        TotalCount = total,
        HasNextPage = page * pageSize < total,
        HasPreviousPage = page > 1
    };
}
