using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Woody.Api.Configuration;
using Woody.Api.RateLimiting;
using Woody.Application.PreLaunch;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/prelaunch")]
public class PreLaunchController : ControllerBase
{
    private const int DailySignupCapPerIpHash = 3;
    private static readonly TimeSpan DailySignupWindow = TimeSpan.FromHours(24);
    private static int _missingDevSecretLogged;
    private static int _missingProdSecretLogged;

    private readonly WoodyDbContext _db;
    private readonly ILogger<PreLaunchController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public PreLaunchController(
        WoodyDbContext db,
        ILogger<PreLaunchController> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("debug/ip")]
    [AllowAnonymous]
    public IActionResult DebugIp()
    {
        var enabled = Environment.GetEnvironmentVariable("WOODY_DEBUG_IP_ENABLED");

        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        return Ok(new
        {
            remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            xForwardedFor = Request.Headers["X-Forwarded-For"].ToString(),
            xRealIp = Request.Headers["X-Real-IP"].ToString(),
            cfConnectingIp = Request.Headers["CF-Connecting-IP"].ToString(),
            forwardedProto = Request.Headers["X-Forwarded-Proto"].ToString(),
            trustPrivateNetworkProxies =
                HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    .GetValue<bool>("ForwardedHeaders:TrustPrivateNetworkProxies")
        });
    }

    [HttpPost("signups")]
    [RequestSizeLimit(16384)]
    [EnableRateLimiting(RateLimitPolicyNames.PreLaunchSignup)]
    public async Task<IActionResult> CreateSignup(
        [FromBody] PreLaunchSignupRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { message = "Requisição inválida." });

        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogDebug("Pre-launch honeypot acionado (descartado).");
            return Ok(new { success = true, message = "Inscrição recebida." });
        }

        if (!request.AcceptedContact)
            return BadRequest(new { message = "É necessário aceitar o contato para concluir a inscrição." });

        var name = PreLaunchSocialInputNormalizer.SanitizeName(request.Name, 120);
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Nome inválido." });

        var normalizedNetwork = PreLaunchSocialInputNormalizer.TryNormalizeNetwork(request.SocialNetwork);
        if (normalizedNetwork is null)
            return BadRequest(new { message = "Rede social inválida." });

        var usernameResult = PreLaunchSocialInputNormalizer.NormalizeUsername(
            normalizedNetwork,
            request.SocialUsername);

        if (!usernameResult.Success || string.IsNullOrWhiteSpace(usernameResult.NormalizedUsername))
            return BadRequest(new { message = usernameResult.Error ?? "Usuário inválido." });

        var displayUsername = usernameResult.DisplayUsername;
        var normalizedUsername = usernameResult.NormalizedUsername;

        var secret = ResolveHashSecret();
        var clientIp = RateLimitClientIp.Get(HttpContext);
        var ipHash = PreLaunchPrivacyHash.Sha256Hex(clientIp, secret);
        var userAgent = Request.Headers.UserAgent.ToString();
        var userAgentHash = PreLaunchPrivacyHash.Sha256Hex(userAgent, secret);

        var existing = await _db.PreLaunchSignups
            .AsNoTracking()
            .AnyAsync(
                s => s.NormalizedSocialNetwork == normalizedNetwork
                     && s.NormalizedSocialUsername == normalizedUsername,
                cancellationToken);

        if (existing)
            return Ok(new { success = true, message = "Inscrição recebida." });

        var since = DateTime.UtcNow - DailySignupWindow;
        var recentCount = await _db.PreLaunchSignups
            .AsNoTracking()
            .CountAsync(
                s => s.IpHash == ipHash && s.CreatedAt >= since,
                cancellationToken);

        if (recentCount >= DailySignupCapPerIpHash)
        {
            Response.Headers.RetryAfter = ((int)DailySignupWindow.TotalSeconds).ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "Muitas tentativas. Tente novamente mais tarde.",
                code = "PRELAUNCH_RATE_LIMITED"
            });
        }

        var signup = new PreLaunchSignup
        {
            Name = name,
            SocialNetwork = normalizedNetwork,
            SocialUsername = displayUsername,
            NormalizedSocialNetwork = normalizedNetwork,
            NormalizedSocialUsername = normalizedUsername,
            IpHash = ipHash,
            UserAgentHash = userAgentHash,
            AcceptedContactAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        _db.PreLaunchSignups.Add(signup);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Ok(new { success = true, message = "Inscrição recebida." });
        }

        _logger.LogInformation("Pre-launch signup registered for network={Network}.", normalizedNetwork);

        return Ok(new { success = true, message = "Inscrição recebida." });
    }

    private string ResolveHashSecret()
    {
        var fromConfig = _configuration["PreLaunch:HashSecret"]
                         ?? Environment.GetEnvironmentVariable("PRELAUNCH_HASH_SECRET");

        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig.Trim();

        if (_environment.IsProduction())
        {
            if (Interlocked.CompareExchange(ref _missingProdSecretLogged, 1, 0) == 0)
            {
                _logger.LogCritical(
                    "PreLaunch:HashSecret / PRELAUNCH_HASH_SECRET não configurado. Defina antes de produção com tráfego real de pré-lançamento.");
            }

            return "unset-prelaunch-production-secret-reconfigure";
        }

        if (Interlocked.CompareExchange(ref _missingDevSecretLogged, 1, 0) == 0)
            _logger.LogWarning("PreLaunch:HashSecret ausente; usando fallback apenas para desenvolvimento.");

        return "dev-only-prelaunch-secret-not-for-production";
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pg)
            return pg.SqlState == PostgresErrorCodes.UniqueViolation;

        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner is PostgresException p)
                return p.SqlState == PostgresErrorCodes.UniqueViolation;
            inner = inner.InnerException;
        }

        return false;
    }
}

public sealed class PreLaunchSignupRequest
{
    public string? Name { get; set; }
    public string? SocialNetwork { get; set; }
    public string? SocialUsername { get; set; }
    public bool AcceptedContact { get; set; }

    /// <summary>Honeypot: se preenchido, o pedido é descartado silenciosamente.</summary>
    public string? Website { get; set; }
}
