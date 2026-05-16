using System.Text;
using Woody.Api.RateLimiting;

namespace Woody.Api.Middlewares;

/// <summary>
/// Lê o corpo JSON (com buffer) antes do rate limiter para expor o e-mail normalizado em <see cref="AuthEmailRateLimitItems.NormalizedEmail"/>.
/// </summary>
public sealed class AuthEmailRateLimitPreparationMiddleware(RequestDelegate next)
{
    private const int MaxBodyBytes = 16_384;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldPrepare(context))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();

        string? normalized = null;
        try
        {
            context.Request.Body.Position = 0;
            var json = await ReadBodyCappedAsync(context.Request.Body, context.RequestAborted).ConfigureAwait(false);
            context.Request.Body.Position = 0;

            var raw = AuthEmailRateLimitBodyParser.TryExtractNormalizedEmail(json);
            if (!string.IsNullOrEmpty(raw) && AuthEmailRateLimitBodyParser.IsPlausibleRateLimitEmail(raw))
                normalized = raw;
        }
        catch
        {
            try
            {
                context.Request.Body.Position = 0;
            }
            catch
            {
                // ignore
            }
        }

        context.Items[AuthEmailRateLimitItems.NormalizedEmail] = normalized;
        await next(context).ConfigureAwait(false);
    }

    private static bool ShouldPrepare(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
            return false;

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        return path.EndsWith("/api/auth/send-verification", StringComparison.Ordinal)
               || path.EndsWith("/api/auth/resend-verification", StringComparison.Ordinal)
               || path.EndsWith("/api/auth/verify-email", StringComparison.Ordinal);
    }

    private static async Task<string> ReadBodyCappedAsync(Stream body, CancellationToken cancellationToken)
    {
        if (!body.CanRead)
            return "";

        var buffer = new byte[MaxBodyBytes];
        var total = 0;
        while (total < MaxBodyBytes)
        {
            var read = await body
                .ReadAsync(buffer.AsMemory(total, MaxBodyBytes - total), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
        }

        return Encoding.UTF8.GetString(buffer, 0, total);
    }
}
