using System.Text.Json;

namespace Woody.Api.Middlewares;

/// <summary>
/// Quando PUBLIC_LAUNCH_MODE=waitlist_form, bloqueia todos os endpoints exceto:
/// - POST /api/prelaunch/signups
/// - OPTIONS (preflight CORS)
/// - GET /health e /health/ready
/// </summary>
public class WaitlistFormModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isWaitlistFormMode;

    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
    };

    public WaitlistFormModeMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var mode = configuration["PUBLIC_LAUNCH_MODE"]
                   ?? Environment.GetEnvironmentVariable("PUBLIC_LAUNCH_MODE")
                   ?? "app";
        _isWaitlistFormMode = mode.Equals("waitlist_form", StringComparison.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_isWaitlistFormMode)
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";

        // Preflight CORS sempre permitido
        if (method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Health checks para Railway/monitoramento
        if (AllowedPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // Único endpoint público: POST /api/prelaunch/signups
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("/api/prelaunch/signups", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status423Locked;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                message = "A Woody está em fase de pré-inscrição.",
                code = "WAITLIST_FORM_MODE"
            }));
    }
}
