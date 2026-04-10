namespace Woody.Application.Interfaces;

public interface ICommunityPermissionService
{
    /// <summary>Membro ativo (owner, admin ou member) pode publicar posts na comunidade.</summary>
    Task<bool> CanPublishPostAsync(int communityId, int userId, CancellationToken cancellationToken = default);

    Task<bool> CanModerateCommunityAsync(int communityId, int userId, CancellationToken cancellationToken = default);
}
