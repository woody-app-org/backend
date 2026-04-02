using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Services;

public class DefaultCommunityBootstrap : IDefaultCommunityBootstrap
{
    private readonly WoodyDbContext _db;

    public DefaultCommunityBootstrap(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task EnsureUserInDefaultCommunityAsync(int userId, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Slug == "geral", cancellationToken);
        if (community == null)
            return;

        var exists = await _db.CommunityMemberships.AnyAsync(
            m => m.CommunityId == community.Id && m.UserId == userId,
            cancellationToken);
        if (exists)
            return;

        _db.CommunityMemberships.Add(new CommunityMembership
        {
            UserId = userId,
            CommunityId = community.Id,
            Role = "member",
            Status = "active",
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        community.MemberCount = await _db.CommunityMemberships.CountAsync(
            m => m.CommunityId == community.Id && m.Status == "active",
            cancellationToken);
        community.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
