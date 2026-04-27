using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Hubs;
using Woody.Application.Configuration;
using Woody.Api.Middlewares;
using Woody.Infrastructure.Persistence.Configuration;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Persistence.Seed;
using Woody.Infrastructure.Security;
using Woody.Infrastructure.Services.Email;

var builder = WebApplication.CreateBuilder(args);

ConfigureRailwayPort(builder);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<WoodyDbContext>(tags: new[] { "ready" });

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Seção Jwt ausente na configuração.");
var resendOptions = builder.Configuration.GetSection("Resend").Get<ResendOptions>()
    ?? throw new InvalidOperationException("Seção Resend ausente na configuração.");
var emailVerificationOptions = builder.Configuration.GetSection("EmailVerification").Get<EmailVerificationOptions>()
    ?? throw new InvalidOperationException("Seção EmailVerification ausente na configuração.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret não configurado. Em produção defina a variável Jwt__Secret (mínimo 32 caracteres recomendado).");
}

if (!builder.Environment.IsDevelopment() && jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret deve ter pelo menos 32 caracteres fora do ambiente Development.");
}

ValidateResendOptions(resendOptions);
ValidateEmailVerificationOptions(emailVerificationOptions);
ValidateJwtOptions(jwtOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR: negotiate (HTTP) envia Bearer via header com accessTokenFactory;
                // WebSocket usa access_token na query. Aceitar ambos em todos os caminhos /hubs/...
                var path = context.HttpContext.Request.Path.Value ?? "";
                if (!path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase)
                    && !path.Equals("/hubs", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                var fromQuery = context.Request.Query["access_token"].ToString();
                if (!string.IsNullOrEmpty(fromQuery))
                {
                    context.Token = fromQuery;
                    return Task.CompletedTask;
                }

                var authHeader = context.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrEmpty(authHeader)
                    && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader["Bearer ".Length..].Trim();
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

WoodyDbConfiguration.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.Configure<EmailVerificationOptions>(builder.Configuration.GetSection("EmailVerification"));
builder.Services.Configure<AuthSecurityOptions>(builder.Configuration.GetSection("AuthSecurity"));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));

var corsEnabled = ConfigureCors(builder);

builder.ResolveDependencyInjection();

var app = builder.Build();

app.UseForwardedHeaders();

if (corsEnabled)
    app.UseCors();

if (ShouldRunDevSeed(app.Environment))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
    DbSeeder.Seed(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<DirectMessagesHub>(DirectMessagesHub.RoutePath).RequireAuthorization();

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready"),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

app.Run();

static void ConfigureRailwayPort(WebApplicationBuilder builder)
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrWhiteSpace(port))
        return;

    if (!int.TryParse(port, out var portNumber) || portNumber <= 0)
        throw new InvalidOperationException($"Variável PORT inválida: {port}.");

    builder.WebHost.UseUrls($"http://0.0.0.0:{portNumber}");
}

static bool ConfigureCors(WebApplicationBuilder builder)
{
    var originsRaw = builder.Configuration["CORS_ORIGINS"]
        ?? Environment.GetEnvironmentVariable("CORS_ORIGINS");

    if (string.IsNullOrWhiteSpace(originsRaw))
    {
        if (builder.Environment.IsDevelopment())
        {
            // SignalR negotiate envia Authorization → preflight CORS; ACAO: * bloqueia no browser.
            // Refletir a origem (localhost:5173, 127.0.0.1, etc.) em vez de AllowAnyOrigin().
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
            return true;
        }

        return false;
    }

    var origins = originsRaw
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(o => o.Length > 0)
        .ToArray();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    return true;
}

static bool ShouldRunDevSeed(IHostEnvironment environment)
{
    if (environment.IsDevelopment())
        return true;

    var flag = Environment.GetEnvironmentVariable("WOODY_ENABLE_DEV_SEED");
    return !string.IsNullOrWhiteSpace(flag)
           && flag.Equals("true", StringComparison.OrdinalIgnoreCase);
}

static void ValidateResendOptions(ResendOptions resendOptions)
{
    if (string.IsNullOrWhiteSpace(resendOptions.ApiKey))
        throw new InvalidOperationException("Resend:ApiKey não configurado.");

    if (string.IsNullOrWhiteSpace(resendOptions.FromEmail))
        throw new InvalidOperationException("Resend:FromEmail não configurado.");
}

static void ValidateEmailVerificationOptions(EmailVerificationOptions options)
{
    if (options.ExpirationMinutes <= 0)
        throw new InvalidOperationException("EmailVerification:ExpirationMinutes deve ser maior que zero.");

    if (options.MaxAttempts <= 0)
        throw new InvalidOperationException("EmailVerification:MaxAttempts deve ser maior que zero.");
}

static void ValidateJwtOptions(JwtOptions options)
{
    if (options.ExpirationMinutes is < 10 or > 15)
        throw new InvalidOperationException("Jwt:ExpirationMinutes deve estar entre 10 e 15 minutos.");
}

public partial class Program { }
