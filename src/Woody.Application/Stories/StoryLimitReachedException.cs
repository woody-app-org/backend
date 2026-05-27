namespace Woody.Application.Stories;

public sealed class StoryLimitReachedException : Exception
{
    public const string ErrorCode = "STORY_LIMIT_REACHED";

    public StoryLimitReachedException()
        : base("Você já tem 3 stories ativos. Aguarde um deles expirar para publicar outro.")
    {
    }
}
