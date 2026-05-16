# Technology Stack

**Analysis Date:** Monday Apr 27, 2026

## Languages

**Primary:**
- C# / .NET 10.0 - API, application, domain, infrastructure, migrations, and tests in `src/Woody.Api/`, `src/Woody.Application/`, `src/Woody.Domain/`, `src/Woody.Infrastructure/`, and `tests/`.

**Secondary:**
- PowerShell - Local helper scripts in `run-api.ps1`, `run-migrations.ps1`, and `scripts/Load-DotEnv.ps1`.
- YAML - Docker Compose and GitHub Actions in `docker-compose.yml` and `.github/workflows/*.yml`.
- JSON - ASP.NET configuration in `src/Woody.Api/appsettings.json`, `src/Woody.Api/appsettings.Development.json`, and `src/Woody.Api/Properties/launchSettings.json`.

## Runtime

**Environment:**
- .NET 10.0 - all projects target `net10.0` in `src/Woody.Api/Woody.Api.csproj`, `src/Woody.Application/Woody.Application.csproj`, `src/Woody.Domain/Woody.Domain.csproj`, `src/Woody.Infrastructure/Woody.Infrastructure.csproj`, and `tests/**/*.csproj`.
- ASP.NET Core runtime 10.0 - production Docker image uses `mcr.microsoft.com/dotnet/aspnet:10.0` in `Dockerfile`.
- .NET SDK 10.0 - build Docker image uses `mcr.microsoft.com/dotnet/sdk:10.0` in `Dockerfile`; CI installs `10.0.x` in `.github/workflows/dotnet.yml`.

**Package Manager:**
- NuGet via `dotnet restore`.
- Lockfile: missing. No `packages.lock.json` detected.

## Frameworks

**Core:**
- ASP.NET Core 10.0 - web API host in `src/Woody.Api/Woody.Api.csproj` and startup pipeline in `src/Woody.Api/Program.cs`.
- ASP.NET Core MVC Controllers - REST endpoints under `src/Woody.Api/Controllers/`.
- SignalR - realtime direct messages via `builder.Services.AddSignalR()` and `app.MapHub<DirectMessagesHub>()` in `src/Woody.Api/Program.cs`; hub implementation in `src/Woody.Api/Hubs/DirectMessagesHub.cs`.
- Entity Framework Core 10.0 - ORM and migrations in `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`, `src/Woody.Infrastructure/Migrations/`, and `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`.
- Npgsql EF Core provider 10.0.0 - PostgreSQL access configured through `.UseNpgsql(...)` in `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`.

**Testing:**
- xUnit 2.9.3 - test runner in `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`, `tests/Woody.Application.Tests/Woody.Application.Tests.csproj`, `tests/Woody.Domain.Tests/Woody.Domain.Tests.csproj`, and `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`.
- Microsoft.NET.Test.Sdk 17.14.1 - test execution in all test projects.
- coverlet.collector 6.0.4 - coverage collector in API, application, and domain test projects.
- Moq 4.20.72 - mocking in `tests/Woody.Application.Tests/Woody.Application.Tests.csproj` and `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`.
- FluentAssertions 8.8.0 - assertion helpers in `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`.
- Microsoft.AspNetCore.Mvc.Testing 10.0.2 - API integration testing in `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`.
- Microsoft.EntityFrameworkCore.InMemory 10.0.3 - in-memory EF testing in `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`.

**Build/Dev:**
- Docker multi-stage build - restore and publish `src/Woody.Api/Woody.Api.csproj` in `Dockerfile`.
- Docker Compose - local PostgreSQL service in `docker-compose.yml`.
- GitHub Actions - restore, build, and test pipeline in `.github/workflows/dotnet.yml`.
- Swashbuckle.AspNetCore 10.1.0 - Swagger generation enabled only in development in `src/Woody.Api/Program.cs`.
- Microsoft.AspNetCore.OpenApi 10.0.1 - OpenAPI package in `src/Woody.Api/Woody.Api.csproj`.
- EF Core design tooling - `Microsoft.EntityFrameworkCore.Design` in `src/Woody.Api/Woody.Api.csproj` and `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.

## Key Dependencies

**Critical:**
- `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.2 - JWT authentication configured in `src/Woody.Api/Program.cs` and token generation in `src/Woody.Infrastructure/Security/JwtTokenService.cs`.
- `Microsoft.EntityFrameworkCore` 10.0.2 - database model, context, and migrations in `src/Woody.Infrastructure/`.
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 - PostgreSQL provider in `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.
- `EFCore.NamingConventions` 10.0.0 - snake_case database naming enabled with `.UseSnakeCaseNamingConvention()` in `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`.
- `Stripe.net` 49.0.0 - checkout, customer portal, subscription reads, and webhook handling in `src/Woody.Infrastructure/Billing/StripePayments/`.

**Infrastructure:**
- `Microsoft.Extensions.Configuration.EnvironmentVariables` 10.0.2 - environment variable configuration in `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.
- `Microsoft.Extensions.Configuration.Json` 10.0.2 - JSON configuration support for design-time EF context in `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbContextFactory.cs`.
- `Microsoft.Extensions.Hosting.Abstractions` 10.0.2 - host environment checks in `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`.
- `Microsoft.Extensions.Options` 10.0.6 - options binding for JWT, Resend, email verification, and billing in `src/Woody.Api/Program.cs`.
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.2 - readiness check for `WoodyDbContext` in `src/Woody.Api/Program.cs`.

## Configuration

**Environment:**
- Configuration sources are ASP.NET Core configuration, JSON files, environment variables, and user secrets for the API project. `src/Woody.Api/Woody.Api.csproj` defines a `UserSecretsId`.
- `.env` file present - local environment configuration, not read.
- `.env.example` file present - example environment configuration, not read.
- `scripts/Load-DotEnv.ps1` loads `.env` key/value pairs into process environment for local PowerShell runs.
- Database connection is required through `ConnectionStrings__DefaultConnection` or `DATABASE_URL`; resolver lives in `src/Woody.Infrastructure/Persistence/Configuration/DatabaseConnectionResolver.cs`.
- Required runtime configuration includes `Jwt__Secret`, `Resend__ApiKey`, `Resend__FromEmail`, and database connection variables, enforced in `src/Woody.Api/Program.cs`.
- Optional/runtime configuration includes `Resend__FromName`, `EmailVerification__ExpirationMinutes`, `EmailVerification__MaxAttempts`, `Billing__Stripe__*`, `CORS_ORIGINS`, `PORT`, `WOODY_ENABLE_DEV_SEED`, and `ASPNETCORE_ENVIRONMENT`.

**Build:**
- Solution: `Woody.sln`.
- API project: `src/Woody.Api/Woody.Api.csproj`.
- Application project: `src/Woody.Application/Woody.Application.csproj`.
- Domain project: `src/Woody.Domain/Woody.Domain.csproj`.
- Infrastructure project: `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.
- Test projects: `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`, `tests/Woody.Application.Tests/Woody.Application.Tests.csproj`, `tests/Woody.Domain.Tests/Woody.Domain.Tests.csproj`, and `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`.
- Docker build config: `Dockerfile`.
- Local service config: `docker-compose.yml`.
- Launch profiles: `src/Woody.Api/Properties/launchSettings.json`.
- GitHub Actions config: `.github/workflows/dotnet.yml`.

## Scripts and Commands

**Local development:**
- `docker compose up -d` - start local PostgreSQL service from `docker-compose.yml`.
- `.\run-api.ps1` - loads `.env`, changes to `src/`, and runs `dotnet run --project .\Woody.Api\ --launch-profile https`.
- `dotnet run --project .\Woody.Api\ --launch-profile http` - documented local HTTP run command in `README.md`.

**Database:**
- `.\run-migrations.ps1` - loads `.env`, changes to `src/`, and runs `dotnet ef database update --project .\Woody.Infrastructure\`.
- `dotnet ef migrations add <Name> --project .\Woody.Infrastructure\` - documented migration creation command in `README.md`.
- `dotnet ef database update <Migration> --project .\Woody.Infrastructure\` - documented rollback/update command in `README.md`.

**CI/build:**
- `dotnet restore` - dependency restore in `.github/workflows/dotnet.yml`.
- `dotnet build --no-restore` - CI build in `.github/workflows/dotnet.yml`.
- `dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"` - CI test command in `.github/workflows/dotnet.yml`.
- Docker publish runs `dotnet publish src/Woody.Api/Woody.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false` in `Dockerfile`.

## Platform Requirements

**Development:**
- .NET SDK 10.x.
- Docker for local PostgreSQL.
- PowerShell or compatible shell for `run-api.ps1`, `run-migrations.ps1`, and `scripts/Load-DotEnv.ps1`.
- PostgreSQL connection through `ConnectionStrings__DefaultConnection` or `DATABASE_URL`.

**Production:**
- Containerized ASP.NET Core application exposing port `8080` in `Dockerfile`.
- `PORT` environment variable is supported for Railway-style deployment in `src/Woody.Api/Program.cs`.
- Production container sets `ASPNETCORE_ENVIRONMENT=Production` and `DOTNET_EnableDiagnostics=0` in `Dockerfile`.
- PostgreSQL is required; external provider not fixed by repository files.
- Hosting platform appears compatible with Railway/Heroku-style `DATABASE_URL` and `PORT`, but no single production platform config file was detected.

---

*Stack analysis: Monday Apr 27, 2026*
