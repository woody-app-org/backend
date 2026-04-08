namespace Woody.Application.DTOs.Api;

public class UserCommunityMembershipDto
{
    public CommunityResponseDto Community { get; set; } = null!;
    public string Role { get; set; } = "";
}
