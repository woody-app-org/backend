using Woody.Application.Interfaces;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public class DefaultCommunityBootstrap : IDefaultCommunityBootstrap
{
    private readonly ICommunityRepository _communities;
    private readonly ICommunityMembershipRepository _memberships;

    public DefaultCommunityBootstrap(ICommunityRepository communities, ICommunityMembershipRepository memberships)
    {
        _communities = communities;
        _memberships = memberships;
    }

    public async Task EnsureUserInDefaultCommunityAsync(int userId, CancellationToken cancellationToken = default)
    {
        var community = await _communities.GetBySlugTrackedAsync("geral", cancellationToken);
        if (community == null)
            return;

        if (await _memberships.ExistsForUserAndCommunityAsync(userId, community.Id, cancellationToken))
            return;

        _memberships.Add(new CommunityMembership
        {
            UserId = userId,
            CommunityId = community.Id,
            Role = "member",
            Status = "active",
            JoinedAt = DateTime.UtcNow
        });

        await _memberships.SaveChangesAsync(cancellationToken);

        community.MemberCount = await _memberships.CountActiveInCommunityAsync(community.Id, cancellationToken);
        community.UpdatedAt = DateTime.UtcNow;
        await _communities.SaveChangesAsync(cancellationToken);
    }
}
