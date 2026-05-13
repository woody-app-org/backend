namespace Woody.Api.RateLimiting;

public static class RateLimitClientIp
{
    public static string Get(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
