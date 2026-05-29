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
            RateLimitPolicyNames.AuthPasswordResetSend => 60,
            RateLimitPolicyNames.AuthPasswordResetVerify => 60,
            RateLimitPolicyNames.AuthPasswordResetConfirm => 60,
            RateLimitPolicyNames.PreLaunchSignup => 3600,
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
        if (policyName == RateLimitPolicyNames.AuthEmailSend
            || policyName == RateLimitPolicyNames.AuthPasswordResetSend)
        {
            message = "Aguarde alguns segundos antes de pedir um novo código.";
            code = "EMAIL_RATE_LIMITED";
        }
        else if (policyName == RateLimitPolicyNames.AuthEmailVerify
                 || policyName == RateLimitPolicyNames.AuthPasswordResetVerify)
        {
            message = "Muitas tentativas. Aguarde um pouco antes de tentar novamente.";
            code = "EMAIL_VERIFY_RATE_LIMITED";
        }
        else if (policyName == RateLimitPolicyNames.AuthPasswordResetConfirm)
        {
            message = "Muitas tentativas. Aguarde um pouco antes de tentar novamente.";
            code = "PASSWORD_RESET_CONFIRM_RATE_LIMITED";
        }
        else if (policyName == RateLimitPolicyNames.PreLaunchSignup)
        {
            message = "Muitas tentativas. Tente novamente mais tarde.";
            code = "PRELAUNCH_RATE_LIMITED";
        }
        else
        {
            message = "Demasiados pedidos. Tente novamente mais tarde.";
            code = "RATE_LIMITED";
        }

        var ipForLog = policyName == RateLimitPolicyNames.PreLaunchSignup ? "(redacted)" : ip;

        logger?.LogWarning(
            "Rate limit atingido. Policy={PolicyName}, Path={Path}, ClientIp={ClientIp}, EmailMask={EmailMask}, EmailHash={EmailHash}, RetryAfterSeconds={RetryAfter}",
            policyName,
            httpContext.Request.Path.Value,
            ipForLog,
            mask ?? "(n/d)",
            hash,
            retrySeconds);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(
            new { message, retryAfterSeconds = retrySeconds, code },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
