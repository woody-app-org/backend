namespace Woody.Application.DTOs.Api;

public sealed class CommunityPremiumDashboardAnalyticsDto
{
    public string CommunityId { get; set; } = null!;
    public string? Slug { get; set; }
    public int PeriodDays { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime PreviousPeriodStartUtc { get; set; }
    public DateTime PreviousPeriodEndUtc { get; set; }

    public int MemberCount { get; set; }
    public int TotalPosts { get; set; }
    public string Headline { get; set; } = "Painel da comunidade";
    public string? Note { get; set; }

    public CommunityAnalyticsPeriodBucketDto Current { get; set; } = null!;
    public CommunityAnalyticsPeriodBucketDto Previous { get; set; } = null!;
    public CommunityEngagementSummaryDto Engagement { get; set; } = null!;
    public IReadOnlyList<CommunityTopPostAnalyticsDto> TopPosts { get; set; } = Array.Empty<CommunityTopPostAnalyticsDto>();
    public IReadOnlyList<CommunityTagCountDto> TopTags { get; set; } = Array.Empty<CommunityTagCountDto>();
    public IReadOnlyList<CommunityDailyActivityPointDto> DailyActivity { get; set; } = Array.Empty<CommunityDailyActivityPointDto>();
}

public sealed class CommunityAnalyticsPeriodBucketDto
{
    public int NewMembersJoined { get; set; }
    public int MemberLeavesRecorded { get; set; }
    public int PageViews { get; set; }
    public int PostsPublished { get; set; }
    public int CommentsPosted { get; set; }
    public int LikesOnPosts { get; set; }
}

public sealed class CommunityEngagementSummaryDto
{
    /// <summary>Média de (gostos + comentários) por post publicado no período.</summary>
    public double AverageInteractionsPerPost { get; set; }

    public string Definition { get; set; } =
        "Interações = gostos em posts da comunidade + comentários em posts da comunidade, criados no período; dividido pelo número de posts publicados no mesmo período.";
}

public sealed class CommunityTopPostAnalyticsDto
{
    public string PostId { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int Score { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
}

public sealed class CommunityTagCountDto
{
    public string Tag { get; set; } = null!;
    public int Count { get; set; }
}

public sealed class CommunityDailyActivityPointDto
{
    public DateOnly DayUtc { get; set; }
    public int Posts { get; set; }
    public int Comments { get; set; }
    public int PageViews { get; set; }
    public int MemberLeaves { get; set; }
    public int NewMembers { get; set; }
}

public sealed class CommunityTopPostAnalyticsRow
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
}

public sealed class CommunityTagCountRow
{
    public string Tag { get; set; } = null!;
    public int Count { get; set; }
}
