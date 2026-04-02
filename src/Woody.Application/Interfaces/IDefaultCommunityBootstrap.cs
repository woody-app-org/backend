namespace Woody.Application.Interfaces;

public interface IDefaultCommunityBootstrap
{
    Task EnsureUserInDefaultCommunityAsync(int userId, CancellationToken cancellationToken = default);
}
