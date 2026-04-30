using System.Text.Json;
using System.Text.Json.Serialization;

namespace Woody.Application.DTOs.Api;

public class NotificationItemDto
{
    public string Id { get; set; } = null!;

    /// <summary>Tipo semântico (snake_case), ex.: <c>post_like</c>.</summary>
    public string Type { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public int? TargetId { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public JsonElement Metadata { get; set; }

    /// <summary>Alias de <see cref="Metadata"/> para compatibilidade com clientes que ainda leem <c>payload</c>.</summary>
    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public UserPublicDto? Actor { get; set; }
}

public class NotificationListResponseDto
{
    public List<NotificationItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class NotificationUnreadCountDto
{
    public int Count { get; set; }
}
