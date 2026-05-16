<!-- refreshed: Monday Apr 27, 2026 -->
# Architecture

**Analysis Date:** Monday Apr 27, 2026

## System Overview

```text
┌─────────────────────────────────────────────────────────────┐
│                  ASP.NET Core API Host                      │
│                  `src/Woody.Api/Program.cs`                 │
├──────────────────┬──────────────────┬───────────────────────┤
│ REST Controllers │ SignalR Hub      │ Middleware/Config     │
│ `src/Woody.Api/  │ `src/Woody.Api/  │ `src/Woody.Api/       │
│ Controllers/`    │ Hubs/`           │ Configuration/`       │
└────────┬─────────┴────────┬─────────┴──────────┬────────────┘
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                 Application Layer                           │
│ `src/Woody.Application/UseCases/`, `Services/`, `Interfaces/`│
│ Orchestrates use cases, authorization decisions, DTO mapping │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│ `src/Woody.Domain/Entities/`, `Subscription/`, `Messaging/`  │
│ Entities, enums, policies, entitlement rules                 │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                        │
│ `src/Woody.Infrastructure/Repositories/`, `Persistence/`,    │
│ `Billing/`, `Security/`, `Services/Email/`                   │
│ EF Core/PostgreSQL, Stripe, JWT, password hashing, Resend    │
└─────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| API host | Builds ASP.NET Core app, validates configuration, wires auth/CORS/Swagger/SignalR/health checks, runs seed, maps controllers and hubs | `src/Woody.Api/Program.cs` |
| REST controllers | HTTP boundary for auth, users, posts, communities, billing, feed, search, reports, join requests, comments, direct messages | `src/Woody.Api/Controllers/` |
| SignalR direct messages | Realtime DM subscriptions and broadcasts by user inbox and conversation groups | `src/Woody.Api/Hubs/DirectMessagesHub.cs`, `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs` |
| Dependency composition | Maps Application interfaces/use cases to Infrastructure implementations | `src/Woody.Api/Configuration/DependencyInjectionConfig.cs` |
| Application use cases | Orchestrates flows such as login, registration, checkout, customer portal, community premium checkout | `src/Woody.Application/UseCases/` |
| Application services | Coordinates domain rules, repositories, DTO mapping, permissions, feed, messaging, analytics, pinning, entitlements | `src/Woody.Application/Services/` |
| Application interfaces | Defines repository, gateway, email, JWT, realtime, billing contracts consumed by API/Application | `src/Woody.Application/Interfaces/` |
| Domain model | Owns entities, enums, messaging policies, post pinning policies, subscription entitlement logic | `src/Woody.Domain/` |
| EF Core persistence | Defines PostgreSQL DbContext, model relationships, constraints, migrations, design-time factory, seed data | `src/Woody.Infrastructure/Persistence/`, `src/Woody.Infrastructure/Migrations/` |
| Repositories | Implements Application interfaces with EF Core queries and tracked/no-tracking access patterns | `src/Woody.Infrastructure/Repositories/` |
| External gateways | Implements Stripe billing, Resend email, JWT token generation, password hashing | `src/Woody.Infrastructure/Billing/`, `src/Woody.Infrastructure/Services/Email/`, `src/Woody.Infrastructure/Security/` |

## Pattern Overview

**Overall:** Layered ASP.NET Core architecture with Clean Architecture-style dependency direction.

**Key Characteristics:**
- `Woody.Api` depends on `Woody.Application` and `Woody.Infrastructure`; it is the composition root in `src/Woody.Api/Program.cs` and `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.
- `Woody.Application` depends only on `Woody.Domain` and expresses infrastructure needs through interfaces in `src/Woody.Application/Interfaces/`.
- `Woody.Infrastructure` depends on `Woody.Application` and `Woody.Domain` to implement repositories and gateways.
- Domain entities and policies live in `src/Woody.Domain/` and are reused by Application and Infrastructure.
- Some endpoints keep business orchestration in controllers, especially larger controllers such as `src/Woody.Api/Controllers/CommunitiesController.cs` and `src/Woody.Api/Controllers/PostsController.cs`.

## Layers

**API Layer:**
- Purpose: Accept HTTP and SignalR traffic, authenticate/authorize requests, parse claims, translate request/response DTOs, select status codes.
- Location: `src/Woody.Api/`
- Contains: `Controllers/`, `Hubs/`, `Middlewares/`, `Configuration/`, `Extensions/`, `Realtime/`, `Program.cs`.
- Depends on: `Woody.Application`, `Woody.Infrastructure`, ASP.NET Core hosting/auth/SignalR/health checks.
- Used by: External clients and the separate frontend repo over REST and SignalR.

**Application Layer:**
- Purpose: Coordinate use cases and application services independent from concrete persistence/gateways.
- Location: `src/Woody.Application/`
- Contains: `UseCases/`, `Services/`, `Interfaces/`, `DTOs/`, `Mapping/`, `Billing/`, `Utilities/`, `Exceptions/`, `Configuration/`.
- Depends on: `Woody.Domain`, Microsoft options abstractions.
- Used by: `src/Woody.Api/Controllers/`, `src/Woody.Api/Hubs/`, and Infrastructure implementations through shared interfaces.

**Domain Layer:**
- Purpose: Hold core data model, enums, policy functions, and entitlement checks.
- Location: `src/Woody.Domain/`
- Contains: `Entities/`, `Entities/Enum/`, `Messaging/`, `Posts/`, `Subscription/`.
- Depends on: Not detected; project file `src/Woody.Domain/Woody.Domain.csproj` has no project references.
- Used by: `src/Woody.Application/` and `src/Woody.Infrastructure/`.

**Infrastructure Layer:**
- Purpose: Implement persistence, external billing/email/security integrations, EF migrations, and database configuration.
- Location: `src/Woody.Infrastructure/`
- Contains: `Repositories/`, `Persistence/`, `Migrations/`, `Billing/`, `Security/`, `Services/Email/`.
- Depends on: `Woody.Application`, `Woody.Domain`, EF Core, Npgsql, Stripe.net, ASP.NET JWT auth.
- Used by: API dependency injection in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs` and database configuration in `src/Woody.Api/Program.cs`.

**Tests Layer:**
- Purpose: Validate each production project with matching test projects.
- Location: `tests/`
- Contains: `tests/Woody.Api.Tests/`, `tests/Woody.Application.Tests/`, `tests/Woody.Domain.Tests/`, `tests/Woody.Infrastructure.Tests/`.
- Depends on: Production projects through test project references.
- Used by: `Woody.sln`.

## Data Flow

### Primary HTTP Request Path

1. ASP.NET Core host starts and registers middleware/services in `src/Woody.Api/Program.cs`.
2. Requests enter middleware: forwarded headers, optional CORS, `ExceptionHandlingMiddleware`, JWT auth, authorization in `src/Woody.Api/Program.cs`.
3. A controller action handles the route, extracts current user with `src/Woody.Api/Extensions/ClaimsExtensions.cs`, validates inputs, and calls Application services/use cases or repositories.
4. Application service/use case enforces application rules and domain policies through files such as `src/Woody.Application/Services/DirectMessagingService.cs`, `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs`, and `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`.
5. Repository/gateway interface calls resolve to Infrastructure implementations registered in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.
6. EF repositories query or mutate PostgreSQL through `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`.
7. DTO mappers in `src/Woody.Application/Mapping/` convert domain entities to response shapes returned by controllers.

### Authentication Flow

1. `POST api/Auth/login` enters `src/Woody.Api/Controllers/AuthController.cs`.
2. `LoginHandler.HandleAsync` in `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs` looks up users via `IUserRepository`, verifies passwords via `IPasswordHasher`, loads subscription state, and calls `IJwtTokenService`.
3. `JwtTokenService` in `src/Woody.Infrastructure/Security/JwtTokenService.cs` generates a JWT using options validated in `src/Woody.Api/Program.cs`.
4. Protected controllers and the SignalR hub use `[Authorize]` and JWT bearer configuration from `src/Woody.Api/Program.cs`.

### Registration Flow

1. `POST api/Auth/register` enters `src/Woody.Api/Controllers/AuthController.cs`.
2. `RegisterHandler` in `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs` validates uniqueness, email verification, birth date, and password hash.
3. `RegisterHandler` persists `User` and `UserSubscription`, then calls `IDefaultCommunityBootstrap`.
4. A JWT is generated for the created user and returned as `LoginResultDTO`.

### Feed/Post Flow

1. Feed reads route through `src/Woody.Api/Controllers/FeedController.cs`; post CRUD and comment/like/pinning routes through `src/Woody.Api/Controllers/PostsController.cs`.
2. Controllers use repositories (`IPostRepository`, `ICommentRepository`, `ILikeRepository`, `ICommunityRepository`) and services (`IFeedService`, `IPostEnrichmentService`, `IContentPinningService`, `ICommunityPermissionService`).
3. Visibility and membership checks use Application services and EF repository queries in `src/Woody.Infrastructure/Repositories/PostRepository.cs`.
4. Responses are shaped by mappers in `src/Woody.Application/Mapping/EntityMappers.cs` and related DTO files under `src/Woody.Application/DTOs/`.

### Billing and Webhook Flow

1. Authenticated checkout routes enter `src/Woody.Api/Controllers/BillingController.cs`.
2. Billing handlers in `src/Woody.Application/UseCases/Billing/` validate plan codes and options, then call `IBillingCheckoutGateway` or `IBillingCustomerPortalGateway`.
3. Stripe gateway implementations live in `src/Woody.Infrastructure/Billing/StripePayments/`.
4. Incoming Stripe webhooks enter unauthenticated `POST api/billing/webhook` in `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`.
5. `StripeBillingWebhookProcessor` in `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs` validates signature, claims idempotency through `IBillingWebhookReceiptRepository`, reads Stripe subscription state, updates user/community subscription rows, and releases claims on transient failure.

### Direct Messaging Realtime Flow

1. REST conversation routes enter `src/Woody.Api/Controllers/ConversationsController.cs`.
2. `DirectMessagingService` in `src/Woody.Application/Services/DirectMessagingService.cs` applies conversation/message policies from `src/Woody.Domain/Messaging/`, persists conversations/messages, and calls `IDirectMessageRealtimePublisher`.
3. `DirectMessageRealtimePublisher` in `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs` emits SignalR events to `DirectMessagesHub` groups.
4. Clients connect to `/hubs/direct-messages` mapped in `src/Woody.Api/Program.cs`; `DirectMessagesHub` verifies participants before adding connections to conversation groups.

**State Management:**
- Primary persisted state is PostgreSQL via EF Core `WoodyDbContext` in `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`.
- Request state is scoped through ASP.NET Core DI and `WoodyDbContext` is registered as scoped in `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`.
- Realtime fanout state is managed by SignalR groups in `src/Woody.Api/Hubs/DirectMessagesHub.cs`; no custom in-memory conversation store detected.
- Startup seed data is inserted by `DbSeeder.Seed` from `src/Woody.Infrastructure/Persistence/Seed/DbSeeder.cs` in Development, or when `WOODY_ENABLE_DEV_SEED=true`.

## Key Abstractions

**Repository Interfaces:**
- Purpose: Hide EF Core implementation from Application/API code.
- Examples: `src/Woody.Application/Interfaces/IPostRepository.cs`, `src/Woody.Application/Interfaces/IUserRepository.cs`, `src/Woody.Application/Interfaces/IConversationRepository.cs`.
- Pattern: Interface in Application, concrete EF repository in `src/Woody.Infrastructure/Repositories/`, registration in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.

**Use Case Handlers:**
- Purpose: Encapsulate multi-step flows that are too large for direct controller logic.
- Examples: `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs`, `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs`, `src/Woody.Application/UseCases/Billing/CreateCheckoutSessionHandler.cs`.
- Pattern: Public `HandleAsync(...)` method with injected interfaces/gateways and DTO return types.

**Application Services:**
- Purpose: Reusable application orchestration for feed, messaging, permissions, analytics, pinning, entitlements.
- Examples: `src/Woody.Application/Services/DirectMessagingService.cs`, `src/Woody.Application/Services/FeedService.cs`, `src/Woody.Application/Services/CommunityPermissionService.cs`.
- Pattern: Service interface in `src/Woody.Application/Interfaces/` or `src/Woody.Application/Services/`, implementation in `src/Woody.Application/Services/`, scoped DI registration.

**Domain Policies:**
- Purpose: Centralize business rules without persistence dependencies.
- Examples: `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`, `src/Woody.Domain/Messaging/DirectMessageMessagePolicy.cs`, `src/Woody.Domain/Posts/PostProfilePinPolicy.cs`, `src/Woody.Domain/Subscription/SubscriptionEntitlement.cs`.
- Pattern: Static or small policy classes consumed by Application services.

**DTO Mappers:**
- Purpose: Convert domain entities into API response models.
- Examples: `src/Woody.Application/Mapping/EntityMappers.cs`, `src/Woody.Application/Mapping/ConversationDtoMapper.cs`, `src/Woody.Application/Mapping/AuthUserMapper.cs`.
- Pattern: Static mapper classes in Application.

## Entry Points

**HTTP API:**
- Location: `src/Woody.Api/Program.cs`
- Triggers: `dotnet run --project .\Woody.Api\ --launch-profile http` from `src/` as documented in `README.md`.
- Responsibilities: Host ASP.NET Core, configure middleware, map controllers, map health endpoints.

**REST Controllers:**
- Location: `src/Woody.Api/Controllers/`
- Triggers: HTTP routes under `/api/...`.
- Responsibilities: Auth, users, posts, communities, billing, Stripe webhooks, conversations, feed, search, reports, join requests, comments.

**SignalR Hub:**
- Location: `src/Woody.Api/Hubs/DirectMessagesHub.cs`
- Triggers: Client connects to `/hubs/direct-messages`.
- Responsibilities: Authorize clients, manage user inbox and conversation groups.

**Health Checks:**
- Location: `src/Woody.Api/Program.cs`
- Triggers: `GET /health`, `GET /health/ready`.
- Responsibilities: Liveness self-check and readiness DbContext check.

**EF Migrations:**
- Location: `src/Woody.Infrastructure/Migrations/`
- Triggers: `run-migrations.ps1` or `dotnet ef database update --project .\Woody.Infrastructure\` from `src/`.
- Responsibilities: PostgreSQL schema evolution for `WoodyDbContext`.

## Architectural Constraints

- **Threading:** ASP.NET Core request/response model and SignalR async handlers; no custom background workers, `IHostedService`, Quartz, Hangfire, timers, or cron jobs detected.
- **Global state:** Static configuration/policy constants exist in files such as `src/Woody.Domain/Messaging/DirectMessageMessagePolicy.cs`, `src/Woody.Domain/Posts/PostProfilePinPolicy.cs`, and `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`. No mutable global application state detected.
- **Circular imports:** Not detected at project reference level. `src/Woody.Application/Woody.Application.csproj` references Domain only; `src/Woody.Infrastructure/Woody.Infrastructure.csproj` references Application and Domain; `src/Woody.Api/Woody.Api.csproj` references Application and Infrastructure.
- **Configuration validation:** `src/Woody.Api/Program.cs` fails startup if JWT, Resend, email verification, or connection string requirements are missing/invalid.
- **Persistence coupling:** Controllers sometimes call repositories and `SaveChangesAsync` directly, so new controller code must respect transaction/order semantics already present in `src/Woody.Api/Controllers/PostsController.cs` and `src/Woody.Api/Controllers/CommunitiesController.cs`.
- **Secrets:** `.env` exists but is not read by this map. Configuration is expected through environment variables, user secrets, or appsettings files.

## Anti-Patterns

### Bypassing Application Interfaces

**What happens:** Controllers and services depend on repository/service interfaces; concrete Infrastructure classes are registered only in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.
**Why it's wrong:** Directly referencing `Woody.Infrastructure` implementations from new Application code would invert the current dependency direction and make tests harder.
**Do this instead:** Add or extend an interface under `src/Woody.Application/Interfaces/`, implement it under `src/Woody.Infrastructure/Repositories/` or `src/Woody.Infrastructure/Billing/`, then register it in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.

### Placing Business Rules Only in Controllers

**What happens:** Some existing routes contain substantial orchestration in `src/Woody.Api/Controllers/CommunitiesController.cs` and `src/Woody.Api/Controllers/PostsController.cs`.
**Why it's wrong:** Repeating this for larger flows spreads domain rules across HTTP endpoints and makes reuse/testing harder.
**Do this instead:** Put reusable or multi-step behavior in `src/Woody.Application/UseCases/` or `src/Woody.Application/Services/`, using domain policies from `src/Woody.Domain/`.

### Persisting Without Existing Repository Save Pattern

**What happens:** Repositories expose `SaveChangesAsync`, and callers decide when to save.
**Why it's wrong:** Adding hidden saves inside new repository methods can break multi-step flows and make partial persistence surprising.
**Do this instead:** Follow patterns in `src/Woody.Infrastructure/Repositories/PostRepository.cs` and call `SaveChangesAsync` explicitly from the orchestrating controller/service/use case.

## Error Handling

**Strategy:** Controllers handle expected domain/application failures locally for some endpoints, while `ExceptionHandlingMiddleware` provides a global fallback mapping.

**Patterns:**
- `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs` maps `UnauthorizedAccessException` to 401, `ForbiddenException` to 403, `InvalidOperationException` to 409, `KeyNotFoundException` to 404, `ArgumentException` to 400, and unexpected exceptions to 500.
- Billing controller methods in `src/Woody.Api/Controllers/BillingController.cs` catch expected exceptions and choose 400/403/404/409 explicitly.
- Stripe webhook processing returns enum outcomes from `IStripeWebhookBillingProcessor` and maps them to HTTP status in `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`.
- Realtime publishing swallows request cancellation and logs warning on SignalR broadcast failure in `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs`.

## Cross-Cutting Concerns

**Logging:** ASP.NET Core `ILogger<T>` is used in `src/Woody.Api/Controllers/AuthController.cs`, `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs`, `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs`, and Stripe processors in `src/Woody.Infrastructure/Billing/StripePayments/`.

**Validation:** Configuration validation occurs at startup in `src/Woody.Api/Program.cs`; request validation is mostly manual in controllers/use cases and helper utilities such as `src/Woody.Application/Utilities/CreateCommunityRequestValidator.cs`.

**Authentication:** JWT bearer auth is configured in `src/Woody.Api/Program.cs`; policies include `AdminOnly`. Controllers use `[Authorize]`, `[AllowAnonymous]`, and `User.GetUserId()` from `src/Woody.Api/Extensions/ClaimsExtensions.cs`. SignalR reads bearer tokens from headers or `access_token` query for `/hubs/...`.

**Authorization:** Role policy is configured in `src/Woody.Api/Program.cs`; resource-level checks are implemented in services and repositories such as `src/Woody.Application/Services/CommunityPermissionService.cs`, `src/Woody.Application/Services/DirectMessagingService.cs`, and membership checks in controllers.

**Persistence:** EF Core/Npgsql is configured in `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs` with snake_case naming and migrations in Infrastructure.

**Deployment/runtime:** `src/Woody.Api/Program.cs` reads `PORT` for Railway-style hosting, applies forwarded headers, conditionally enables Swagger in Development, and uses HSTS/HTTPS redirection outside Development.

---

*Architecture analysis: Monday Apr 27, 2026*
