using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Media;

namespace Woody.Application.Mapping;

public static class MessageDtoMapper
{
    public static MessageResponseDto ToResponse(Message message)
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
                ? Array.Empty<MessageAttachmentResponseDto>()
                : message.Attachments
                    .OrderBy(a => a.DisplayOrder)
                    .ThenBy(a => a.Id)
                    .Select(a => new MessageAttachmentResponseDto
                    {
                        Id = a.Id,
                        Url = a.Url,
                        MediaType = MediaKindApi.ToApiString(a.MediaKind),
                        ContentType = a.ContentType,
                        DurationSeconds = a.DurationSeconds,
                        DisplayOrder = a.DisplayOrder,
                        CreatedAt = a.CreatedAt
                    })
                    .ToList()
        };
    }

    public static IReadOnlyList<MessageResponseDto> ToResponseList(IEnumerable<Message> messages) =>
        messages.Select(ToResponse).ToList();
}
