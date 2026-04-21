using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IMessageRepository
{
    /// <summary>Última mensagem visível por conversa (uma entrada por <paramref name="conversationIds"/> com mensagens).</summary>
    Task<IReadOnlyDictionary<int, (string? Preview, DateTime AtUtc)>> GetLastMessageSummariesByConversationIdsAsync(
        IReadOnlyList<int> conversationIds,
        CancellationToken cancellationToken = default);

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
