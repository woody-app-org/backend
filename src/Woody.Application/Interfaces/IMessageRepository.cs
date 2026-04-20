using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IMessageRepository
{
    Task<(List<Message> Items, int Total)> ListByConversationPagedAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Message?> GetTrackedInConversationAsync(
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default);

    Task<Message?> GetNoTrackingByIdInConversationAsync(
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default);

    void Add(Message message);

    void RemoveAttachments(IEnumerable<MessageAttachment> attachments);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
