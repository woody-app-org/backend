using Woody.Domain.Entities;

namespace Woody.Application.Interfaces.Security
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, UserSubscription? subscription);
    }
}