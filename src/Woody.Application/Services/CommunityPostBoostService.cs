using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class CommunityPostBoostService : ICommunityPostBoostService
{
    private readonly ICommunityPremiumEntitlementService _entitlements;
    private readonly IPostRepository _posts;
    private readonly ICommunityPostBoostRepository _boosts;

    public CommunityPostBoostService(
        ICommunityPremiumEntitlementService entitlements,
        IPostRepository posts,
        ICommunityPostBoostRepository boosts)
    {
        _entitlements = entitlements;
        _posts = posts;
        _boosts = boosts;
    }

    public async Task<(CommunityPostBoostResponseDto? dto, string? error)> ActivateAsync(
        int communityId,
        int postId,
        int actorUserId,
        int? durationDays,
        CancellationToken cancellationToken = default)
    {
        var caps = await _entitlements.GetCapabilitiesAsync(communityId, actorUserId, cancellationToken);
        if (!caps.IsStaffForPremiumTools)
            return (null, "community_staff_required");
        if (!caps.CommunityPremiumActive || !caps.CanBoostCommunityPosts)
            return (null, "community_premium_required");

        var post = await _posts.GetByIdNonDeletedWithNavAsync(postId, cancellationToken);
        if (post == null)
            return (null, "post_not_found");
        if (post.PublicationContext != PostPublicationContext.Community || post.CommunityId != communityId)
            return (null, "post_not_in_community");

        var days = Math.Clamp(durationDays ?? 7, 1, 14);
        var now = DateTime.UtcNow;
        var ends = now.AddDays(days);

        await _boosts.CancelActiveForPostAsync(postId, now, cancellationToken);

        var row = new CommunityPostBoost
        {
            PostId = postId,
            CommunityId = communityId,
            StartedAtUtc = now,
            EndsAtUtc = ends,
            CancelledAtUtc = null,
            CreatedAtUtc = now
        };
        _boosts.Add(row);
        await _boosts.SaveChangesAsync(cancellationToken);

        return (ToResponseDto(row, now), null);
    }

    public async Task<(bool ok, string? error)> DeactivateAsync(
        int communityId,
        int postId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var caps = await _entitlements.GetCapabilitiesAsync(communityId, actorUserId, cancellationToken);
        if (!caps.IsStaffForPremiumTools)
            return (false, "community_staff_required");
        if (!caps.CommunityPremiumActive || !caps.CanBoostCommunityPosts)
            return (false, "community_premium_required");

        var post = await _posts.GetByIdNonDeletedWithNavAsync(postId, cancellationToken);
        if (post == null)
            return (false, "post_not_found");
        if (post.PublicationContext != PostPublicationContext.Community || post.CommunityId != communityId)
            return (false, "post_not_in_community");

        var now = DateTime.UtcNow;
        await _boosts.CancelActiveForPostAsync(postId, now, cancellationToken);
        await _boosts.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(IReadOnlyList<CommunityPostBoostListItemDto> items, string? error)> ListActiveAsync(
        int communityId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var caps = await _entitlements.GetCapabilitiesAsync(communityId, actorUserId, cancellationToken);
        if (!caps.IsStaffForPremiumTools)
            return (Array.Empty<CommunityPostBoostListItemDto>(), "community_staff_required");
        if (!caps.CommunityPremiumActive || !caps.CanBoostCommunityPosts)
            return (Array.Empty<CommunityPostBoostListItemDto>(), "community_premium_required");

        var now = DateTime.UtcNow;
        var rows = await _boosts.ListActiveForCommunityAsync(communityId, now, 30, cancellationToken);
        var items = rows.Select(b => new CommunityPostBoostListItemDto
        {
            Id = b.Id.ToString(),
            PostId = b.PostId.ToString(),
            PostContentPreview = EntityMappers.ToPostContentPreview(b.Post?.Content),
            StartedAtUtc = EntityMappers.Iso(b.StartedAtUtc),
            EndsAtUtc = EntityMappers.Iso(b.EndsAtUtc),
            Active = b.IsActiveAt(now)
        }).ToList();

        return (items, null);
    }

    private static CommunityPostBoostResponseDto ToResponseDto(CommunityPostBoost b, DateTime utcNow) =>
        new()
        {
            Id = b.Id.ToString(),
            PostId = b.PostId.ToString(),
            CommunityId = b.CommunityId.ToString(),
            StartedAtUtc = EntityMappers.Iso(b.StartedAtUtc),
            EndsAtUtc = EntityMappers.Iso(b.EndsAtUtc),
            Active = b.IsActiveAt(utcNow)
        };
}
