using System.Net;

namespace Woody.Api.RateLimiting;

public static class RateLimitClientIp
{
    /// <summary>
    /// Resolve o IP real do cliente considerando proxies e load balancers.
    /// Ordem de prioridade:
    ///   1. CF-Connecting-IP  (Cloudflare, se presente)
    ///   2. X-Forwarded-For   (primeiro IP da lista — o do cliente original)
    ///   3. X-Real-IP
    ///   4. HttpContext.Connection.RemoteIpAddress
    ///   5. "unknown"
    ///
    /// Nota: X-Forwarded-For é processado pelo UseForwardedHeaders() do ASP.NET Core, que
    /// valida a cadeia de proxies confiáveis (KnownIPNetworks) antes de atualizar RemoteIpAddress.
    /// O fallback explícito para o header bruto só é atingido se o middleware não processar o header
    /// (ex.: proxy fora de KnownIPNetworks).
    /// </summary>
    public static string Get(HttpContext httpContext)
    {
        var cfConnectingIp = httpContext.Request.Headers["CF-Connecting-IP"].ToString();
        if (IsValidIp(cfConnectingIp))
            return cfConnectingIp.Trim();

        var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xForwardedFor))
        {
            var firstIp = xForwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (IsValidIp(firstIp))
                return firstIp!;
        }

        var xRealIp = httpContext.Request.Headers["X-Real-IP"].ToString();
        if (IsValidIp(xRealIp))
            return xRealIp.Trim();

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (IsValidIp(remoteIp))
            return remoteIp!;

        return "unknown";
    }

    private static bool IsValidIp(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value.Trim(), out _);
}
