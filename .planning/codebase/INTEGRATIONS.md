# External Integrations

**Analysis Date:** Monday Apr 27, 2026

## APIs & External Services

**Billing and subscriptions:**
- Stripe - subscription checkout, customer billing portal, subscription state reads, and billing webhooks.
  - SDK/Client: `Stripe.net` 49.0.0 in `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.
  - Checkout gateway: `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCheckoutGateway.cs`.
  - Customer portal gateway: `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCustomerPortalGateway.cs`.
  - Subscription gateway: `src/Woody.Infrastructure/Billing/Stripe/StripeBillingSubscriptionGateway.cs`.
  - Webhook processor: `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`.
  - Webhook endpoint: `POST /api/billing/webhook` in `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`.
  - Auth/config: `Billing__Stripe__SecretKey`, `Billing__Stripe__WebhookSecret`, `Billing__Stripe__PriceIds__ProMonthly`, `Billing__Stripe__PriceIds__ProAnnual`, `Billing__Stripe__PriceIds__CommunityPremiumMonthly`, `Billing__Stripe__PriceIds__CommunityPremiumAnnual`.
  - Redirect/config URLs: `Billing__Stripe__CheckoutSuccessUrl`, `Billing__Stripe__CheckoutCancelUrl`, `Billing__Stripe__CustomerPortalReturnUrl`, `Billing__Stripe__CustomerPortalConfigurationId`.

**Email delivery:**
- Resend - email verification delivery.
  - SDK/Client: typed `HttpClient` with base address `https://api.resend.com/` in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`; no Resend-specific NuGet SDK detected.
  - Implementation: `src/Woody.Infrastructure/Services/Email/ResendEmailSender.cs`.
  - Options: `src/Woody.Infrastructure/Services/Email/ResendOptions.cs`.
  - Auth/config: `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName`.

**Realtime API:**
- SignalR - authenticated direct message realtime hub.
  - SDK/Client: ASP.NET Core SignalR from framework runtime; no separate package detected.
  - Endpoint: `/hubs/direct-messages` in `src/Woody.Api/Hubs/DirectMessagesHub.cs`.
  - Auth: JWT bearer token; `src/Woody.Api/Program.cs` accepts tokens from `Authorization: Bearer` and `access_token` query string for `/hubs/*`.

**Developer notifications:**
- Discord webhooks - GitHub Actions notifications for CI failures, opened/reopened issues, and opened/reopened/ready PRs.
  - Client: `curl` from GitHub Actions workflows.
  - Implementation: `.github/workflows/dotnet.yml`, `.github/workflows/notify-issues.yml`, and `.github/workflows/notify-pr.yml`.
  - Auth/config: GitHub Actions secrets `DISCORD_WEBHOOK_URL_CI`, `DISCORD_WEBHOOK_URL_ISSUES`, and `DISCORD_WEBHOOK_URL_PR`.

## Data Storage

**Databases:**
- PostgreSQL
  - Local service: `postgres:18` in `docker-compose.yml`.
  - ORM/client: EF Core with `Npgsql.EntityFrameworkCore.PostgreSQL` in `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.
  - DbContext: `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`.
  - Runtime configuration: `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`.
  - Design-time configuration: `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbContextFactory.cs`.
  - Connection: `ConnectionStrings__DefaultConnection` or `DATABASE_URL`; resolved by `src/Woody.Infrastructure/Persistence/Configuration/DatabaseConnectionResolver.cs`.
  - Schema: `public`; enforced by appending `Search Path=public` when missing in `DatabaseConnectionResolver`.
  - Migrations: `src/Woody.Infrastructure/Migrations/`.

**File Storage:**
- Not detected. No storage-specific services, S3/Azure Blob integrations, or storage classes were found under `src/`.
- Post image metadata exists in the EF model (`PostImages` in `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`), but no external file storage integration was detected.

**Caching:**
- Not detected. No Redis, memory cache abstraction, or distributed cache package usage found.

**Queues / Background Jobs:**
- Not detected. No RabbitMQ, MassTransit, Hangfire, Quartz, `IHostedService`, or `BackgroundService` usage found under `src/`.

## Authentication & Identity

**Auth Provider:**
- Custom JWT authentication.
  - Implementation: `src/Woody.Infrastructure/Security/JwtTokenService.cs`.
  - Options: `src/Woody.Infrastructure/Security/JwtOptions.cs`.
  - API configuration: `src/Woody.Api/Program.cs`.
  - Package: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.2 in `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.
  - Required secret: `Jwt__Secret`.
  - Configurable issuer/audience/expiry: `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` through the `Jwt` config section.
  - Authorization: policy `AdminOnly` in `src/Woody.Api/Program.cs`; controller-level `[Authorize]` usage such as `src/Woody.Api/Controllers/BillingController.cs`.

**Password Handling:**
- Custom password hashing abstraction registered as `IPasswordHasher` -> `PasswordHasher` in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.
- Implementation path: `src/Woody.Infrastructure/Security/PasswordHasher.cs`.

**Email Verification:**
- Email verification settings are configured with `EmailVerification__ExpirationMinutes` and `EmailVerification__MaxAttempts`.
- Options are validated in `src/Woody.Api/Program.cs`.
- Email verification service is registered in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.

## Monitoring & Observability

**Error Tracking:**
- Not detected. No Sentry, Application Insights, OpenTelemetry, or external error tracking package usage found.

**Logs:**
- Built-in Microsoft logging is configured through `src/Woody.Api/appsettings.json` and `src/Woody.Api/appsettings.Development.json`.
- Stripe webhook processing uses `ILogger<StripeBillingWebhookProcessor>` in `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`.
- No structured logging provider such as Serilog was detected.

**Health Checks:**
- Liveness endpoint: `/health` in `src/Woody.Api/Program.cs`.
- Readiness endpoint: `/health/ready` with EF Core DbContext check in `src/Woody.Api/Program.cs`.
- Package: `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` in `src/Woody.Api/Woody.Api.csproj`.

## CI/CD & Deployment

**Hosting:**
- Docker container deployment supported by `Dockerfile`.
- Runtime container exposes port `8080`.
- `PORT` environment variable is honored in `src/Woody.Api/Program.cs`, which supports Railway/Heroku-style dynamic ports.
- Production database provider is not fixed by repository files; PostgreSQL connection is provided through environment variables.

**CI Pipeline:**
- GitHub Actions CI in `.github/workflows/dotnet.yml`.
  - Restores with `dotnet restore`.
  - Builds with `dotnet build --no-restore`.
  - Tests with `dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"`.
  - Sends Discord notification on failure through `DISCORD_WEBHOOK_URL_CI`.
- GitHub Actions issue notifications in `.github/workflows/notify-issues.yml`.
- GitHub Actions PR notifications in `.github/workflows/notify-pr.yml`.

## Environment Configuration

**Required env vars:**
- `ConnectionStrings__DefaultConnection` or `DATABASE_URL` - PostgreSQL connection.
- `Jwt__Secret` - JWT signing secret; enforced at startup in `src/Woody.Api/Program.cs`.
- `Resend__ApiKey` - Resend bearer token; enforced at startup in `src/Woody.Api/Program.cs`.
- `Resend__FromEmail` - email sender address; enforced at startup in `src/Woody.Api/Program.cs`.

**Integration-specific env vars:**
- `Resend__FromName` - optional sender display name.
- `EmailVerification__ExpirationMinutes` - verification code lifetime.
- `EmailVerification__MaxAttempts` - max verification attempts.
- `Billing__Stripe__SecretKey` - Stripe API key for checkout, portal, and subscription reads.
- `Billing__Stripe__WebhookSecret` - Stripe webhook signature verification secret.
- `Billing__Stripe__CheckoutSuccessUrl` - Stripe checkout success redirect URL.
- `Billing__Stripe__CheckoutCancelUrl` - Stripe checkout cancellation redirect URL.
- `Billing__Stripe__CustomerPortalReturnUrl` - Stripe customer portal return URL.
- `Billing__Stripe__CustomerPortalConfigurationId` - optional Stripe billing portal configuration id.
- `Billing__Stripe__PriceIds__ProMonthly` - Stripe price id for user Pro monthly.
- `Billing__Stripe__PriceIds__ProAnnual` - Stripe price id for user Pro annual.
- `Billing__Stripe__PriceIds__CommunityPremiumMonthly` - Stripe price id for community premium monthly.
- `Billing__Stripe__PriceIds__CommunityPremiumAnnual` - Stripe price id for community premium annual.
- `CORS_ORIGINS` - comma-separated allowed origins outside the development default behavior.
- `PORT` - runtime HTTP port for container platforms.
- `WOODY_ENABLE_DEV_SEED` - enables development seed outside Development when set to true.
- `ASPNETCORE_ENVIRONMENT` - ASP.NET runtime environment.

**Secrets location:**
- `.env` file present - local secrets/configuration, not read.
- `.env.example` file present - example configuration, not read.
- ASP.NET user secrets configured for `src/Woody.Api/Woody.Api.csproj`.
- GitHub Actions secrets used for Discord webhook URLs in `.github/workflows/*.yml`.
- Production secrets are expected through environment variables; no production secret store config was detected.

## Webhooks & Callbacks

**Incoming:**
- `POST /api/billing/webhook` - Stripe billing webhook in `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`.
  - Allows anonymous requests.
  - Reads raw request body.
  - Uses `Stripe-Signature` header.
  - Processor validates `Billing__Stripe__WebhookSecret`.
  - Handles `checkout.session.completed`, `customer.subscription.created`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.paid`, and `invoice.payment_failed` in `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`.
  - Uses `BillingWebhookReceipt` storage to claim Stripe event ids and avoid duplicate processing.

**Outgoing:**
- Resend email API: `POST https://api.resend.com/emails` through `src/Woody.Infrastructure/Services/Email/ResendEmailSender.cs`.
- Stripe API calls:
  - Customer create and checkout session create in `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCheckoutGateway.cs`.
  - Billing portal session create in `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCustomerPortalGateway.cs`.
  - Subscription read in `src/Woody.Infrastructure/Billing/Stripe/StripeBillingSubscriptionGateway.cs`.
- Discord webhook calls from GitHub Actions in `.github/workflows/dotnet.yml`, `.github/workflows/notify-issues.yml`, and `.github/workflows/notify-pr.yml`.
- Stripe redirects:
  - Checkout success/cancel URLs configured under `Billing:Stripe` in `src/Woody.Api/appsettings.json`.
  - Customer portal return URL configured under `Billing:Stripe` in `src/Woody.Api/appsettings.json`.

---

*Integration audit: Monday Apr 27, 2026*
