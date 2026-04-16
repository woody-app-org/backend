using Woody.Application.DTOs;
using Woody.Domain.Entities;

namespace Woody.Application.Mapping;

public static class AuthUserMapper
{
    public static AuthUserDto From(User user, UserSubscription? subscription, DateTime utcNow) => new()
    {
        Id = user.Id.ToString(),
        Username = user.Username,
        Email = user.Email,
        IsEmailVerified = user.IsEmailVerified,
        Name = user.DisplayName ?? user.Username,
        AvatarUrl = user.ProfilePic,
        Subscription = SubscriptionDtoMapper.ToStateDto(subscription, utcNow)
    };
}
