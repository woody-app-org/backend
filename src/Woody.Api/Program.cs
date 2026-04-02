using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Woody.Api.Configuration;
using Woody.Api.Middlewares;
using Woody.Infrastructure.Persistence.Configuration;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Persistence.Seed;
using Woody.Infrastructure.Security;

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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

WoodyDbConfiguration.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

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
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
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
                .AllowAnyMethod();
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
