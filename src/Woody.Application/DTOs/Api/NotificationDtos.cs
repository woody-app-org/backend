using System.Text.Json;
using System.Text.Json.Serialization;

namespace Woody.Application.DTOs.Api;

/// <summary>Ator da notificação (subconjunto público, nomes estáveis para o cliente).</summary>
public sealed class NotificationActorDto
{
    public string Id { get; set; } = null!;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = null!;

    public string Username { get; set; } = null!;

    /// <summary>URL do avatar (equivalente a <see cref="UserPublicDto.AvatarUrl"/>).</summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    public static NotificationActorDto? FromUserPublic(UserPublicDto? u)
    {
        if (u == null)
            return null;

        return new NotificationActorDto
        {
            Id = u.Id,
            DisplayName = u.Name,
            Username = u.Username,
            Avatar = u.AvatarUrl
        };
    }
}

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

    /// <summary>Alias de <see cref="Metadata"/> para clientes legados que ainda leem <c>payload</c>.</summary>
    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public NotificationActorDto? Actor { get; set; }
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
