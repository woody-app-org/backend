# Codebase Structure

**Analysis Date:** Monday Apr 27, 2026

## Directory Layout

```text
backend/
├── Woody.sln                         # Solution with src and tests projects
├── README.md                         # Local setup, migrations, run instructions
├── docker-compose.yml                # Local PostgreSQL service definition
├── run-api.ps1                       # API run helper
├── run-migrations.ps1                # EF migration helper
├── scripts/
│   └── Load-DotEnv.ps1               # Loads .env values into PowerShell session
├── src/
│   ├── Woody.Api/                    # ASP.NET Core host, controllers, hubs, middleware
│   ├── Woody.Application/            # Use cases, services, DTOs, interfaces, mappers
│   ├── Woody.Domain/                 # Entities, enums, domain policies
│   └── Woody.Infrastructure/         # EF Core, repositories, migrations, gateways
├── tests/
│   ├── Woody.Api.Tests/              # API tests
│   ├── Woody.Application.Tests/      # Application layer tests
│   ├── Woody.Domain.Tests/           # Domain policy/model tests
│   └── Woody.Infrastructure.Tests/   # Infrastructure tests
└── .planning/
    └── codebase/                     # Generated codebase mapping documents
```

## Directory Purposes

**Root (`backend/`):**
- Purpose: Repository root for the .NET backend solution.
- Contains: `Woody.sln`, setup docs, Docker Compose, PowerShell helper scripts, `src/`, `tests/`, `.planning/`.
- Key files: `Woody.sln`, `README.md`, `docker-compose.yml`, `run-api.ps1`, `run-migrations.ps1`.

**`src/Woody.Api/`:**
- Purpose: ASP.NET Core Web API and realtime host.
- Contains: Entry point, controllers, SignalR hubs, middleware, config extension methods, claim helpers, realtime publisher.
- Key files: `src/Woody.Api/Program.cs`, `src/Woody.Api/Woody.Api.csproj`, `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`, `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs`.

**`src/Woody.Api/Controllers/`:**
- Purpose: HTTP route boundary.
- Contains: Controllers for auth, users, posts, communities, billing, Stripe webhooks, conversations, feed, search, reports, join requests, comments.
- Key files: `src/Woody.Api/Controllers/AuthController.cs`, `src/Woody.Api/Controllers/PostsController.cs`, `src/Woody.Api/Controllers/CommunitiesController.cs`, `src/Woody.Api/Controllers/BillingController.cs`, `src/Woody.Api/Controllers/ConversationsController.cs`, `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`.

**`src/Woody.Api/Hubs/`:**
- Purpose: SignalR realtime endpoint definitions.
- Contains: Direct message hub and group naming helper.
- Key files: `src/Woody.Api/Hubs/DirectMessagesHub.cs`, `src/Woody.Api/Hubs/DirectMessageHubGroups.cs`.

**`src/Woody.Api/Realtime/`:**
- Purpose: API-side SignalR publisher implementation for Application realtime abstraction.
- Contains: Publisher that broadcasts direct-message events to hub groups.
- Key files: `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs`.

**`src/Woody.Api/Configuration/`:**
- Purpose: API composition helpers.
- Contains: DI registration extension.
- Key files: `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.

**`src/Woody.Application/`:**
- Purpose: Application layer and contracts.
- Contains: Use cases, services, interfaces, DTOs, mappings, billing helpers, validators, exceptions, options.
- Key files: `src/Woody.Application/Woody.Application.csproj`.

**`src/Woody.Application/UseCases/`:**
- Purpose: Multi-step workflows invoked by controllers.
- Contains: Auth and billing handlers.
- Key files: `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs`, `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs`, `src/Woody.Application/UseCases/Billing/CreateCheckoutSessionHandler.cs`, `src/Woody.Application/UseCases/Billing/CreateCommunityPremiumCheckoutSessionHandler.cs`, `src/Woody.Application/UseCases/Billing/CreateCustomerPortalSessionHandler.cs`.

**`src/Woody.Application/Services/`:**
- Purpose: Reusable application orchestration.
- Contains: Feed, post enrichment, direct messaging, permissions, entitlements, community analytics, post boost, content pinning.
- Key files: `src/Woody.Application/Services/DirectMessagingService.cs`, `src/Woody.Application/Services/FeedService.cs`, `src/Woody.Application/Services/PostEnrichmentService.cs`, `src/Woody.Application/Services/CommunityPermissionService.cs`, `src/Woody.Application/Services/ContentPinningService.cs`.

**`src/Woody.Application/Interfaces/`:**
- Purpose: Ports used by Application/API to avoid coupling to concrete Infrastructure.
- Contains: Repository interfaces, billing interfaces, email interfaces, security interfaces, realtime publisher interface.
- Key files: `src/Woody.Application/Interfaces/IPostRepository.cs`, `src/Woody.Application/Interfaces/IUserRepository.cs`, `src/Woody.Application/Interfaces/IConversationRepository.cs`, `src/Woody.Application/Interfaces/Billing/IBillingCheckoutGateway.cs`, `src/Woody.Application/Interfaces/Security/IJwtTokenService.cs`.

**`src/Woody.Application/DTOs/`:**
- Purpose: Request/response shapes shared by API and Application.
- Contains: API DTOs, billing DTOs, auth/user/community/post/message DTOs.
- Key files: `src/Woody.Application/DTOs/Api/PostResponseDto.cs`, `src/Woody.Application/DTOs/Api/CommunityResponseDto.cs`, `src/Woody.Application/DTOs/Billing/CreateBillingCheckoutRequestDto.cs`.

**`src/Woody.Application/Mapping/`:**
- Purpose: Converts domain entities to DTOs.
- Contains: Static mapper classes for auth, entities, subscriptions, conversations, messages, billing.
- Key files: `src/Woody.Application/Mapping/EntityMappers.cs`, `src/Woody.Application/Mapping/AuthUserMapper.cs`, `src/Woody.Application/Mapping/ConversationDtoMapper.cs`, `src/Woody.Application/Mapping/MessageDtoMapper.cs`.

**`src/Woody.Application/Billing/`:**
- Purpose: Billing domain support at application level.
- Contains: Plan catalogs, plan codes, Stripe metadata keys, subscription sync helpers, billing read models.
- Key files: `src/Woody.Application/Billing/BillingPlanCatalog.cs`, `src/Woody.Application/Billing/CommunityBillingPlanCodes.cs`, `src/Woody.Application/Billing/StripeBillingMetadataKeys.cs`, `src/Woody.Application/Billing/UserSubscriptionStripeSync.cs`, `src/Woody.Application/Billing/CommunitySubscriptionStripeSync.cs`.

**`src/Woody.Application/Utilities/`:**
- Purpose: Shared input helpers and validators.
- Contains: Community request validation and slug generation.
- Key files: `src/Woody.Application/Utilities/CreateCommunityRequestValidator.cs`, `src/Woody.Application/Utilities/CommunitySlugHelper.cs`.

**`src/Woody.Domain/`:**
- Purpose: Core model and pure domain rules.
- Contains: Entities, enums, messaging policies, post policies, subscription entitlement logic.
- Key files: `src/Woody.Domain/Woody.Domain.csproj`.

**`src/Woody.Domain/Entities/`:**
- Purpose: EF-backed domain entities.
- Contains: Users, posts, comments, likes, follows, communities, memberships, reports, subscriptions, billing receipts, conversations, messages.
- Key files: `src/Woody.Domain/Entities/User.cs`, `src/Woody.Domain/Entities/Post.cs`, `src/Woody.Domain/Entities/Community.cs`, `src/Woody.Domain/Entities/Conversation.cs`, `src/Woody.Domain/Entities/Message.cs`, `src/Woody.Domain/Entities/UserSubscription.cs`, `src/Woody.Domain/Entities/CommunitySubscription.cs`.

**`src/Woody.Domain/Entities/Enum/`:**
- Purpose: Domain enumerations.
- Contains: Billing, subscription, community plan, publication context, conversation status, like target enums.
- Key files: `src/Woody.Domain/Entities/Enum/SubscriptionPlan.cs`, `src/Woody.Domain/Entities/Enum/SubscriptionStatus.cs`, `src/Woody.Domain/Entities/Enum/CommunityPlan.cs`, `src/Woody.Domain/Entities/Enum/PostPublicationContext.cs`.

**`src/Woody.Domain/Messaging/`:**
- Purpose: Direct-message business policies.
- Contains: Conversation ordering/status rules, message rules, attachment rules.
- Key files: `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`, `src/Woody.Domain/Messaging/DirectMessageMessagePolicy.cs`, `src/Woody.Domain/Messaging/DirectMessageAttachmentPolicy.cs`.

**`src/Woody.Domain/Posts/`:**
- Purpose: Post/comment pinning policies.
- Contains: Profile pin and comment pin rules.
- Key files: `src/Woody.Domain/Posts/PostProfilePinPolicy.cs`, `src/Woody.Domain/Posts/PostCommentPinPolicy.cs`.

**`src/Woody.Domain/Subscription/`:**
- Purpose: Subscription entitlement rules.
- Contains: User/community entitlement checks and premium feature gate.
- Key files: `src/Woody.Domain/Subscription/SubscriptionEntitlement.cs`, `src/Woody.Domain/Subscription/CommunitySubscriptionEntitlement.cs`, `src/Woody.Domain/Subscription/CommunityPremiumFeatureGate.cs`.

**`src/Woody.Infrastructure/`:**
- Purpose: Concrete persistence and external service implementation.
- Contains: Repositories, EF Core DbContext/config/factory/seed, migrations, Stripe billing, JWT/password security, email sender.
- Key files: `src/Woody.Infrastructure/Woody.Infrastructure.csproj`.

**`src/Woody.Infrastructure/Persistence/`:**
- Purpose: Database access setup and seed data.
- Contains: `Context/`, `Configuration/`, `Seed/`.
- Key files: `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`, `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`, `src/Woody.Infrastructure/Persistence/Configuration/DatabaseConnectionResolver.cs`, `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbContextFactory.cs`, `src/Woody.Infrastructure/Persistence/Seed/DbSeeder.cs`.

**`src/Woody.Infrastructure/Repositories/`:**
- Purpose: EF Core implementations of repository interfaces.
- Contains: Repositories for users, posts, comments, likes, follows, communities, memberships, join requests, subscriptions, messages, conversations, reports, analytics, billing receipts.
- Key files: `src/Woody.Infrastructure/Repositories/PostRepository.cs`, `src/Woody.Infrastructure/Repositories/UserRepository.cs`, `src/Woody.Infrastructure/Repositories/CommunityRepository.cs`, `src/Woody.Infrastructure/Repositories/ConversationRepository.cs`, `src/Woody.Infrastructure/Repositories/MessageRepository.cs`.

**`src/Woody.Infrastructure/Billing/`:**
- Purpose: Stripe implementation details.
- Contains: Checkout, customer portal, subscription gateway, webhook processor, signature verifier, mappers.
- Key files: `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCheckoutGateway.cs`, `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCustomerPortalGateway.cs`, `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`, `src/Woody.Infrastructure/Billing/Stripe/StripeBillingSubscriptionGateway.cs`, `src/Woody.Infrastructure/Billing/Stripe/StripeBillingWebhookSignatureVerifier.cs`.

**`src/Woody.Infrastructure/Security/`:**
- Purpose: Authentication support implementation.
- Contains: JWT options/service and password hasher.
- Key files: `src/Woody.Infrastructure/Security/JwtOptions.cs`, `src/Woody.Infrastructure/Security/JwtTokenService.cs`, `src/Woody.Infrastructure/Security/PasswordHasher.cs`.

**`src/Woody.Infrastructure/Services/Email/`:**
- Purpose: Resend email implementation and options.
- Contains: Email sender and options.
- Key files: `src/Woody.Infrastructure/Services/Email/ResendEmailSender.cs`, `src/Woody.Infrastructure/Services/Email/ResendOptions.cs`.

**`src/Woody.Infrastructure/Migrations/`:**
- Purpose: EF Core migration history and generated model snapshot.
- Contains: Migration classes and `WoodyDbContextModelSnapshot.cs`.
- Key files: `src/Woody.Infrastructure/Migrations/WoodyDbContextModelSnapshot.cs`.

**`tests/`:**
- Purpose: Test projects matching production projects.
- Contains: `Woody.Api.Tests`, `Woody.Application.Tests`, `Woody.Domain.Tests`, `Woody.Infrastructure.Tests`.
- Key files: `tests/Woody.Domain.Tests/DirectMessageConversationPolicyTests.cs`, `tests/Woody.Domain.Tests/PostProfilePinPolicyTests.cs`, `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`, `tests/Woody.Application.Tests/Woody.Application.Tests.csproj`.

## Key File Locations

**Entry Points:**
- `src/Woody.Api/Program.cs`: Application startup, middleware, auth, DI, CORS, health checks, SignalR, seeding, run lifecycle.
- `src/Woody.Api/Controllers/*.cs`: REST API endpoint entry points.
- `src/Woody.Api/Hubs/DirectMessagesHub.cs`: SignalR realtime entry point at `/hubs/direct-messages`.
- `run-api.ps1`: Script entry point for running the API.
- `run-migrations.ps1`: Script entry point for applying EF migrations.

**Configuration:**
- `Woody.sln`: Solution membership for source and test projects.
- `src/Woody.Api/Woody.Api.csproj`: ASP.NET Core app project and project references.
- `src/Woody.Application/Woody.Application.csproj`: Application project; references Domain.
- `src/Woody.Domain/Woody.Domain.csproj`: Domain project with no project references.
- `src/Woody.Infrastructure/Woody.Infrastructure.csproj`: Infrastructure project; references Application and Domain.
- `src/Woody.Api/appsettings.json`: API appsettings file present; contents not needed for architecture map.
- `src/Woody.Api/appsettings.Development.json`: Development appsettings file present; contents not needed for architecture map.
- `src/Woody.Api/Properties/launchSettings.json`: Launch profiles.
- `.env`: Local environment file present; not read because it may contain secrets.
- `.env.example`: Environment example file present; not read during this architecture map.
- `docker-compose.yml`: Local PostgreSQL compose file present; password-bearing sections not read during this architecture map.

**Core Logic:**
- `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs`: Login flow.
- `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs`: Registration flow.
- `src/Woody.Application/UseCases/Billing/CreateCheckoutSessionHandler.cs`: User subscription checkout flow.
- `src/Woody.Application/UseCases/Billing/CreateCommunityPremiumCheckoutSessionHandler.cs`: Community premium checkout flow.
- `src/Woody.Application/Services/DirectMessagingService.cs`: Direct-message conversation/message flow.
- `src/Woody.Application/Services/FeedService.cs`: Feed orchestration.
- `src/Woody.Application/Services/CommunityPermissionService.cs`: Community authorization decisions.
- `src/Woody.Application/Services/ContentPinningService.cs`: Post/comment pinning behavior.
- `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`: Direct-message conversation policy.
- `src/Woody.Domain/Subscription/SubscriptionEntitlement.cs`: User subscription entitlement policy.
- `src/Woody.Domain/Subscription/CommunitySubscriptionEntitlement.cs`: Community subscription entitlement policy.

**Persistence:**
- `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`: DbSets, EF relationships, indexes, constraints.
- `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`: Npgsql/EF Core service registration.
- `src/Woody.Infrastructure/Persistence/Configuration/DatabaseConnectionResolver.cs`: Connection string and `DATABASE_URL` resolution.
- `src/Woody.Infrastructure/Persistence/Seed/DbSeeder.cs`: Development seed data.
- `src/Woody.Infrastructure/Repositories/*.cs`: EF repository implementations.
- `src/Woody.Infrastructure/Migrations/*.cs`: EF migrations.

**External Integrations:**
- `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`: Stripe webhook processing.
- `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCheckoutGateway.cs`: Stripe checkout.
- `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingCustomerPortalGateway.cs`: Stripe customer portal.
- `src/Woody.Infrastructure/Billing/Stripe/StripeBillingSubscriptionGateway.cs`: Stripe subscription reads.
- `src/Woody.Infrastructure/Services/Email/ResendEmailSender.cs`: Resend email sending.
- `src/Woody.Infrastructure/Security/JwtTokenService.cs`: JWT issuance.

**Testing:**
- `tests/Woody.Api.Tests/`: API tests.
- `tests/Woody.Application.Tests/`: Application tests.
- `tests/Woody.Domain.Tests/`: Domain tests.
- `tests/Woody.Infrastructure.Tests/`: Infrastructure tests.

## Naming Conventions

**Files:**
- Controllers use PascalCase ending in `Controller.cs`: `src/Woody.Api/Controllers/PostsController.cs`.
- Interfaces use `I` prefix and often live in `src/Woody.Application/Interfaces/`: `src/Woody.Application/Interfaces/IPostRepository.cs`.
- Services use `*Service.cs`: `src/Woody.Application/Services/DirectMessagingService.cs`.
- Use case handlers use `*Handler.cs`: `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs`.
- Repositories use `*Repository.cs`: `src/Woody.Infrastructure/Repositories/PostRepository.cs`.
- DTOs use `*Dto.cs` or existing uppercase `*DTO.cs` variants: `src/Woody.Application/DTOs/Api/PostResponseDto.cs`, `src/Woody.Application/DTOs/CreateCommunityRequestDTO.cs`.
- EF migrations use timestamped generated filenames: `src/Woody.Infrastructure/Migrations/20260423201709_AddCommunityPostBoosts.cs`.

**Directories:**
- Project directories use `Woody.<Layer>`: `src/Woody.Api/`, `src/Woody.Application/`, `src/Woody.Domain/`, `src/Woody.Infrastructure/`.
- Test project directories mirror production layers: `tests/Woody.Domain.Tests/`.
- API feature boundaries are mostly controller files in `src/Woody.Api/Controllers/`.
- Application feature boundaries are split by technical role: `UseCases/`, `Services/`, `Interfaces/`, `DTOs/`, `Mapping/`.

## Where to Add New Code

**New HTTP Endpoint:**
- Controller action: add to an existing controller under `src/Woody.Api/Controllers/` when it fits an existing route group.
- New route group: create a new `*Controller.cs` under `src/Woody.Api/Controllers/`.
- Request/response DTOs: add under `src/Woody.Application/DTOs/` or a feature subfolder such as `src/Woody.Application/DTOs/Api/` or `src/Woody.Application/DTOs/Billing/`.
- Business flow: add a use case under `src/Woody.Application/UseCases/<Feature>/` for multi-step operations; use a service under `src/Woody.Application/Services/` for reusable behavior.
- Tests: add matching tests under `tests/Woody.Api.Tests/` and/or `tests/Woody.Application.Tests/`.

**New Domain Rule:**
- Pure business rule/policy: add under `src/Woody.Domain/<Feature>/` when independent of persistence and HTTP.
- Entity or enum: add under `src/Woody.Domain/Entities/` or `src/Woody.Domain/Entities/Enum/`.
- Tests: add focused tests under `tests/Woody.Domain.Tests/`.

**New Persistence Operation:**
- Interface: add or extend a contract under `src/Woody.Application/Interfaces/`.
- Implementation: add or extend a repository under `src/Woody.Infrastructure/Repositories/`.
- DI registration: register the interface and implementation in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`.
- Schema change: update entities/DbContext and add EF migration under `src/Woody.Infrastructure/Migrations/`.
- Tests: add repository or integration tests under `tests/Woody.Infrastructure.Tests/`.

**New External Integration:**
- Application port: add an interface under `src/Woody.Application/Interfaces/` or a subfolder such as `src/Woody.Application/Interfaces/Billing/`.
- Concrete client/gateway: implement under `src/Woody.Infrastructure/<Integration>/` or existing folders such as `src/Woody.Infrastructure/Billing/`.
- Options/configuration model: place cross-layer options in `src/Woody.Application/Configuration/` when Application needs values, or Infrastructure-specific options in the relevant Infrastructure folder.
- DI registration: wire in `src/Woody.Api/Configuration/DependencyInjectionConfig.cs` or `src/Woody.Api/Program.cs` if host-level configuration is needed.

**New Realtime Direct Message Behavior:**
- Hub methods/group rules: use `src/Woody.Api/Hubs/DirectMessagesHub.cs` and `src/Woody.Api/Hubs/DirectMessageHubGroups.cs`.
- Application event abstraction: extend `src/Woody.Application/Interfaces/IDirectMessageRealtimePublisher.cs`.
- SignalR implementation: update `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs`.
- Domain rules: place pure messaging constraints in `src/Woody.Domain/Messaging/`.

**New Billing Behavior:**
- Plan/catalog metadata: use `src/Woody.Application/Billing/`.
- Checkout/customer portal use cases: use `src/Woody.Application/UseCases/Billing/`.
- Stripe implementation: use `src/Woody.Infrastructure/Billing/StripePayments/` or `src/Woody.Infrastructure/Billing/Stripe/` according to the existing split.
- Webhook route changes: use `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`.

**Utilities:**
- Shared application validation/transforms: `src/Woody.Application/Utilities/`.
- API-only helpers: `src/Woody.Api/Extensions/` or `src/Woody.Api/Configuration/`.
- Infrastructure configuration helpers: `src/Woody.Infrastructure/Persistence/Configuration/` or the relevant Infrastructure feature folder.

## Special Directories

**`src/*/bin/` and `src/*/obj/`:**
- Purpose: .NET build outputs and generated intermediate files.
- Generated: Yes.
- Committed: Not intended.

**`tests/*/bin/` and `tests/*/obj/`:**
- Purpose: Test build outputs and generated intermediate files.
- Generated: Yes.
- Committed: Not intended.

**`src/Woody.Infrastructure/Migrations/`:**
- Purpose: EF Core migration source files that define database schema evolution.
- Generated: Partly, by EF Core migration tooling.
- Committed: Yes.

**`.planning/codebase/`:**
- Purpose: GSD codebase map documents consumed by planning/execution workflows.
- Generated: Yes, by mapping workflow.
- Committed: Project-dependent; no commit was made by this mapping.

**`.cursor/skills/`:**
- Purpose: Project GSD workflow skills.
- Generated: Managed tooling.
- Committed: Project-dependent; not modified by this mapping.

**`.env`:**
- Purpose: Local environment configuration and secrets.
- Generated: Developer-local.
- Committed: No; file exists but contents were not read.

---

*Structure analysis: Monday Apr 27, 2026*
