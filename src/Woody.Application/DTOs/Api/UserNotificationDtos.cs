using System.Text.Json;

namespace Woody.Application.DTOs.Api;

public class UserNotificationItemDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public UserPublicDto? Actor { get; set; }
    public JsonElement Payload { get; set; }
}

public class UserNotificationListResponseDto
{
    public List<UserNotificationItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UserNotificationUnreadCountDto
{
    public int Count { get; set; }
}
