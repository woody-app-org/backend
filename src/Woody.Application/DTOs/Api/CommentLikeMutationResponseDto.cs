namespace Woody.Application.DTOs.Api;

/// <summary>Estado de gostos num comentário após curtir/descurtir.</summary>
public sealed class CommentLikeMutationResponseDto
{
    public int LikesCount { get; set; }
    public bool LikedByCurrentUser { get; set; }
}
