using Woody.Domain.Entities.Enum;

namespace Woody.Application.DTOs;

/// <summary>
/// Dados mínimos para filtrar e ordenar feeds sem carregar posts completos.
/// </summary>
public sealed record PostFeedCandidate(
    int Id,
    int UserId,
    PostPublicationContext PublicationContext,
    int? CommunityId,
    DateTime CreatedAt,
    int CommunityMemberCountSnapshot);
