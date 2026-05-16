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

    public async Task<IReadOnlyDictionary<int, (string? Preview, DateTime AtUtc)>> GetLastMessageSummariesByConversationIdsAsync(
        IReadOnlyList<int> conversationIds,
        CancellationToken cancellationToken = default)
    {
        if (conversationIds.Count == 0)
            return new Dictionary<int, (string? Preview, DateTime AtUtc)>();

        var rows = await _db.Messages.AsNoTracking()
            .Include(m => m.MediaAttachments)
            .Where(m => conversationIds.Contains(m.ConversationId) && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var dict = new Dictionary<int, (string? Preview, DateTime AtUtc)>();
        foreach (var g in rows.GroupBy(m => m.ConversationId))
        {
            var m = g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).First();
            dict[g.Key] = (BuildPreview(m), m.CreatedAt);
        }

        return dict;
    }

    private static string? BuildPreview(Message m)
    {
        if (!string.IsNullOrWhiteSpace(m.Body))
        {
            var t = m.Body.Trim();
            return t.Length <= 120 ? t : t[..120] + "…";
        }

        return m.MediaAttachments.Count > 0 ? "Mídia" : null;
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
            .Include(m => m.MediaAttachments)
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
            .Include(m => m.MediaAttachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);

    public Task<Message?> GetNoTrackingByIdInConversationAsync(
        int conversationId,
        int messageId,
        CancellationToken cancellationToken = default) =>
        _db.Messages.AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.MediaAttachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);

    public void Add(Message message) => _db.Messages.Add(message);

    public void RemoveMediaAttachments(IEnumerable<MediaAttachment> attachments) =>
        _db.MediaAttachments.RemoveRange(attachments);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
