using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly WoodyDbContext _db;

    public MessageRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task<(List<Message> Items, int Total)> ListByConversationPagedAsync(
        int conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId);

        var total = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Message?> GetTrackedInConversationAsync(
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default) =>
        _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);

    public Task<Message?> GetNoTrackingByIdInConversationAsync(
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default) =>
        _db.Messages.AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);

    public void Add(Message message) => _db.Messages.Add(message);

    public void RemoveAttachments(IEnumerable<MessageAttachment> attachments) =>
        _db.MessageAttachments.RemoveRange(attachments);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
