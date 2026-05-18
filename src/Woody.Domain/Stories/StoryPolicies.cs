namespace Woody.Domain.Stories;

public static class StoryPolicies
{
    public const int MaxActiveStoriesPerUser = 3;
    public static readonly TimeSpan StoryLifetime = TimeSpan.FromHours(24);

    public const int MaxTextLength = 500;
    public const int MaxBackgroundColorLength = 32;
}
