using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetTrackedByPairAsync(int userLowId, int userHighId, CancellationToken cancellationToken = default);

    Task<Conversation?> GetTrackedByIdForParticipantAsync(
        int conversationId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<Conversation>> ListMineNoTrackingAsync(int userId, CancellationToken cancellationToken = default);

    Task<List<Conversation>> ListPendingInboundNoTrackingAsync(int userId, CancellationToken cancellationToken = default);

    void Add(Conversation conversation);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
