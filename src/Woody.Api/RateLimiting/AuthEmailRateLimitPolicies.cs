using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Woody.Api.RateLimiting;

public static class AuthEmailRateLimitPolicies
{
    /// <summary>
    /// Envio/reenvio: token bucket 1 req / 60 s + janela fixa 5 / 15 min por e-mail normalizado.
    /// Sem e-mail válido no corpo: limite por IP (evita partilha global com chave vazia).
    /// </summary>
    public static RateLimitPartition<string> PartitionAuthEmailSend(HttpContext httpContext)
    {
        var ip = RateLimitClientIp.Get(httpContext);
        var email = httpContext.Items[AuthEmailRateLimitItems.NormalizedEmail] as string;
        if (string.IsNullOrEmpty(email))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"send-invalid:{ip}",
                _ => CreateFixedWindowOptions(30, TimeSpan.FromMinutes(10)));
        }

        return RateLimitPartition.Get(
            $"send:{email}",
            _ => RateLimiter.CreateChained(
                new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1,
                    TokensPerPeriod = 1,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(60),
                    AutoReplenishment = true,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }),
                new FixedWindowRateLimiter(CreateFixedWindowOptions(5, TimeSpan.FromMinutes(15)))));
    }

    /// <summary>
    /// Confirmação do código: 10 tentativas / 10 min por e-mail; fallback por IP se o corpo não tiver e-mail.
    /// </summary>
    public static RateLimitPartition<string> PartitionAuthEmailVerify(HttpContext httpContext)
    {
        var ip = RateLimitClientIp.Get(httpContext);
        var email = httpContext.Items[AuthEmailRateLimitItems.NormalizedEmail] as string;
        if (string.IsNullOrEmpty(email))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"verify-invalid:{ip}",
                _ => CreateFixedWindowOptions(40, TimeSpan.FromMinutes(10)));
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            $"verify:{email}",
            _ => CreateFixedWindowOptions(10, TimeSpan.FromMinutes(10)));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(int permitLimit, TimeSpan window) => new()
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
        AutoReplenishment = true
    };
}
