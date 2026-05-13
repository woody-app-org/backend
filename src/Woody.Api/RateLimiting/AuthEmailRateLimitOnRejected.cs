using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;

namespace Woody.Api.RateLimiting;

public static class AuthEmailRateLimitOnRejected
{
    public static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (httpContext.Response.HasStarted)
            return;

        var loggerFactory = httpContext.RequestServices.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("Woody.Api.RateLimiting");

        var attr = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        var policyName = attr?.PolicyName ?? "unknown";

        var ip = RateLimitClientIp.Get(httpContext);
        var emailNorm = httpContext.Items[AuthEmailRateLimitItems.NormalizedEmail] as string;
        string? mask = null;
        int? hash = null;
        if (!string.IsNullOrEmpty(emailNorm))
        {
            mask = AuthEmailRateLimitBodyParser.MaskEmailForLog(emailNorm);
            hash = AuthEmailRateLimitBodyParser.StableHashForLog(emailNorm);
        }

        var retrySeconds = policyName switch
        {
            RateLimitPolicyNames.AuthEmailSend => 60,
            RateLimitPolicyNames.AuthEmailVerify => 60,
            _ => 30
        };

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra))
        {
            var sec = (int)Math.Ceiling(ra.TotalSeconds);
            if (sec > 0)
                retrySeconds = sec;
        }

        if (retrySeconds > 0)
            httpContext.Response.Headers.RetryAfter = retrySeconds.ToString(NumberFormatInfo.InvariantInfo);

        string message;
        string code;
        if (policyName == RateLimitPolicyNames.AuthEmailSend)
        {
            message = "Aguarde alguns segundos antes de pedir um novo código.";
            code = "EMAIL_RATE_LIMITED";
        }
        else if (policyName == RateLimitPolicyNames.AuthEmailVerify)
        {
            message = "Muitas tentativas. Aguarde um pouco antes de tentar novamente.";
            code = "EMAIL_VERIFY_RATE_LIMITED";
        }
        else
        {
            message = "Demasiados pedidos. Tente novamente mais tarde.";
            code = "RATE_LIMITED";
        }

        logger?.LogWarning(
            "Rate limit atingido. Policy={PolicyName}, Path={Path}, ClientIp={ClientIp}, EmailMask={EmailMask}, EmailHash={EmailHash}, RetryAfterSeconds={RetryAfter}",
            policyName,
            httpContext.Request.Path.Value,
            ip,
            mask ?? "(n/d)",
            hash,
            retrySeconds);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(
            new { message, retryAfterSeconds = retrySeconds, code },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
