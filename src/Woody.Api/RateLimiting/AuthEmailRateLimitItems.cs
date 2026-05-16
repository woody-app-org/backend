namespace Woody.Api.RateLimiting;

/// <summary>
/// Itens populados por <see cref="Middlewares.AuthEmailRateLimitPreparationMiddleware"/> antes do rate limiter.
/// </summary>
public static class AuthEmailRateLimitItems
{
    public const string NormalizedEmail = "woody:auth:rate_limit:normalized_email";
}
