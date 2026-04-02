namespace Woody.Application.DTOs.Api;

public class CommunityMemberItemDto
{
    public UserPublicDto User { get; set; } = null!;
    public string Role { get; set; } = null!;
}
