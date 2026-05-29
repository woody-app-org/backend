using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Media;

namespace Woody.Application.Mapping;

public static class MessageDtoMapper
{
    public static MessageResponseDto ToResponse(Message message, SharedPostPreviewDto? sharedPost = null)
    {
        var deleted = message.DeletedAt != null;
        return new MessageResponseDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Sender = new MessageAuthorResponseDto
            {
                Id = message.Sender.Id,
                Username = message.Sender.Username,
                DisplayName = message.Sender.DisplayName,
                ProfilePic = message.Sender.ProfilePic
            },
            Body = deleted ? null : message.Body,
            CreatedAt = message.CreatedAt,
            EditedAt = deleted ? null : message.EditedAt,
            DeletedAt = message.DeletedAt,
            IsEdited = !deleted && message.EditedAt != null,
            IsDeleted = deleted,
            Attachments = deleted
                ? Array.Empty<MediaAttachmentResponseDto>()
                : message.MediaAttachments
                    .OrderBy(a => a.DisplayOrder)
                    .ThenBy(a => a.Id)
                    .Select(MediaAttachmentDtoMapper.ToResponseDto)
                    .ToList(),
            SharedPost = deleted ? null : sharedPost
        };
    }

    public static IReadOnlyList<MessageResponseDto> ToResponseList(
        IEnumerable<Message> messages,
        IReadOnlyDictionary<int, SharedPostPreviewDto?>? sharedPostsByMessageId = null) =>
        messages.Select(m =>
        {
            SharedPostPreviewDto? preview = null;
            if (sharedPostsByMessageId != null && sharedPostsByMessageId.TryGetValue(m.Id, out var p))
                preview = p;
            return ToResponse(m, preview);
        }).ToList();
}
