using Woody.Application.DTOs.Api;

namespace Woody.Application.Services;

public interface ICommunityDashboardAnalyticsService
{
    Task<CommunityPremiumDashboardAnalyticsDto> BuildDashboardAsync(
        int communityId,
        string? slug,
        int periodDays,
        CancellationToken cancellationToken = default);
}
