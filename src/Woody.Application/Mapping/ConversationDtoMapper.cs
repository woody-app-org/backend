using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Mapping;

public static class ConversationDtoMapper
{
    public static string StatusToApi(ConversationStatus status) =>
        status switch
        {
            ConversationStatus.Pending => "pending",
            ConversationStatus.Accepted => "accepted",
            ConversationStatus.Rejected => "rejected",
            _ => status.ToString().ToLowerInvariant()
        };

    public static ConversationResponseDto ToResponse(Conversation c, int viewerUserId)
    {
        var other = c.UserLowId == viewerUserId ? c.UserHigh : c.UserLow;
        return new ConversationResponseDto
        {
            Id = c.Id,
            Status = StatusToApi(c.Status),
            UserLowId = c.UserLowId,
            UserHighId = c.UserHighId,
            InitiatorUserId = c.InitiatorUserId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            RespondedAt = c.RespondedAt,
            OtherUser = new ConversationPeerPreviewDto
            {
                Id = other.Id,
                Username = other.Username,
                DisplayName = other.DisplayName,
                ProfilePic = other.ProfilePic
            }
        };
    }

    public static IReadOnlyList<ConversationResponseDto> ToResponseList(IEnumerable<Conversation> items, int viewerUserId) =>
        items.Select(c => ToResponse(c, viewerUserId)).ToList();

    public static ConversationRealtimeDto ToRealtime(Conversation c) =>
        new()
        {
            Id = c.Id,
            Status = StatusToApi(c.Status),
            UserLowId = c.UserLowId,
            UserHighId = c.UserHighId,
            InitiatorUserId = c.InitiatorUserId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            RespondedAt = c.RespondedAt
        };
}
