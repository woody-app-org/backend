namespace Woody.Application.Interfaces;

public interface ICommunityPermissionService
{
    Task<bool> CanModerateCommunityAsync(int communityId, int userId, CancellationToken cancellationToken = default);
}
