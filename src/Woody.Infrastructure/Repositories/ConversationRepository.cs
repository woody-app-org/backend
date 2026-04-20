using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly WoodyDbContext _db;

    public ConversationRepository(WoodyDbContext db)
    {
        _db = db;
    }

    private static IQueryable<Conversation> WithPeerUsers(IQueryable<Conversation> q) =>
        q.Include(c => c.UserLow)
            .Include(c => c.UserHigh)
            .Include(c => c.Initiator);

    public Task<Conversation?> GetTrackedByPairAsync(int userLowId, int userHighId, CancellationToken cancellationToken = default) =>
        WithPeerUsers(_db.Conversations)
            .FirstOrDefaultAsync(c => c.UserLowId == userLowId && c.UserHighId == userHighId, cancellationToken);

    public Task<Conversation?> GetTrackedByIdForParticipantAsync(
        int conversationId,
        int userId,
        CancellationToken cancellationToken = default) =>
        WithPeerUsers(_db.Conversations)
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && (c.UserLowId == userId || c.UserHighId == userId),
                cancellationToken);

    public Task<bool> IsParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default) =>
        _db.Conversations.AsNoTracking()
            .AnyAsync(
                c => c.Id == conversationId && (c.UserLowId == userId || c.UserHighId == userId),
                cancellationToken);

    public async Task<List<Conversation>> ListMineNoTrackingAsync(int userId, CancellationToken cancellationToken = default) =>
        await WithPeerUsers(_db.Conversations.AsNoTracking())
            .Where(c =>
                (c.UserLowId == userId || c.UserHighId == userId)
                && c.Status != ConversationStatus.Rejected
                && (c.Status == ConversationStatus.Accepted
                    || (c.Status == ConversationStatus.Pending && c.InitiatorUserId == userId)))
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

    public async Task<List<Conversation>> ListPendingInboundNoTrackingAsync(int userId, CancellationToken cancellationToken = default) =>
        await WithPeerUsers(_db.Conversations.AsNoTracking())
            .Where(c =>
                (c.UserLowId == userId || c.UserHighId == userId)
                && c.Status == ConversationStatus.Pending
                && c.InitiatorUserId != null
                && c.InitiatorUserId != userId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public void Add(Conversation conversation) => _db.Conversations.Add(conversation);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
