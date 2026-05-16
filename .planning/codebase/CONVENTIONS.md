# Coding Conventions

**Analysis Date:** Monday Apr 27, 2026

## Naming Patterns

**Files:**
- Use C# type-name files: `src/Woody.Api/Controllers/PostsController.cs`, `src/Woody.Application/Services/DirectMessagingService.cs`, `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`.
- Controllers end in `Controller` and live under `src/Woody.Api/Controllers/`.
- Services end in `Service` and interfaces use the `I*` prefix under `src/Woody.Application/Interfaces/`.
- Domain policy classes end in `Policy`, for example `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs` and `src/Woody.Domain/Posts/PostCommentPinPolicy.cs`.
- DTO naming is mixed: newer API DTOs usually use `Dto` (`src/Woody.Application/DTOs/Api/ConversationResponseDto.cs`), while older DTOs use `DTO` (`src/Woody.Application/DTOs/UpdateProfileRequestDTO.cs`). Prefer `Dto` for new API DTOs unless editing an existing `DTO` family.

**Functions:**
- Public methods use PascalCase: `StartOrGetConversationAsync` in `src/Woody.Application/Services/DirectMessagingService.cs`.
- Async methods end in `Async` when they return `Task` or `Task<T>`: `IncrementPageViewAsync` in `src/Woody.Infrastructure/Repositories/CommunityDailyRollupRepository.cs`.
- Private helpers use PascalCase and stay near their caller: `ViewerCanReadPostAsync`, `FromPinningOutcome`, and `NormalizePostImageUrls` in `src/Woody.Api/Controllers/PostsController.cs`.
- Domain policy methods are imperative or predicate-style: `OrderParticipantPair`, `MaySendMessage`, `CanPinCommentOnPost`.

**Variables:**
- Private fields use `_camelCase`: `_posts`, `_communities`, `_directMessaging`, `_db`.
- Locals use `camelCase`: `viewerId`, `imageUrls`, `utcNow`, `conversationId`.
- Constants use PascalCase for private constants: `MaxPostImages`, `MaxMessageBodyLength`, `MaxAttachmentUrlLength`.

**Types:**
- Classes, records, interfaces, enums, and DTOs use PascalCase.
- Interfaces start with `I`: `src/Woody.Application/Interfaces/ICommunityPermissionService.cs`.
- Request/response DTOs include action and shape in the name: `StartConversationRequestDto`, `ConversationMessagesPageDto`, `CommunityPremiumDashboardAnalyticsDto`.
- Domain entities are simple singular nouns: `Post`, `Comment`, `Conversation`, `CommunityDailyRollup`.

## Code Style

**Formatting:**
- Tool used: Not detected. No `.editorconfig`, `.prettierrc`, `csharpier`, `dotnet format` config, or repo-level style config found.
- Use .NET SDK defaults with 4-space indentation, braces on their own line for classes/methods, and compact one-line guard clauses where already used.
- Use file-scoped namespaces for new code where possible, matching most newer files such as `src/Woody.Api/Controllers/ConversationsController.cs` and `src/Woody.Application/Services/DirectMessagingService.cs`.
- Some older files use block-scoped namespaces (`src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs`, `src/Woody.Infrastructure/Persistence/Context/WoodyDbContext.cs`). Preserve the local style when editing those files.

**Linting:**
- Tool used: Not detected. No StyleCop, Roslynator, `TreatWarningsAsErrors`, `AnalysisLevel`, or `EnforceCodeStyleInBuild` settings found in `*.csproj`.
- Nullable reference types and implicit usings are enabled across projects via `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in `src/Woody.Api/Woody.Api.csproj`, `src/Woody.Application/Woody.Application.csproj`, `src/Woody.Domain/Woody.Domain.csproj`, and test project files.
- Use `dotnet build` as the primary compile/style gate because no separate linter is configured.

## Import Organization

**Order:**
1. Framework namespaces first: `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore`, `System.*`.
2. Application namespaces next: `Woody.Api.*`, `Woody.Application.*`.
3. Domain/infrastructure namespaces last when needed: `Woody.Domain.*`, `Woody.Infrastructure.*`.

**Path Aliases:**
- Not applicable for C#. Project references are declared in `Woody.sln` and individual `*.csproj` files.
- Use namespaces that mirror the layer and folder: `Woody.Api.Controllers`, `Woody.Application.Services`, `Woody.Domain.Messaging`, `Woody.Infrastructure.Repositories`.

## Error Handling

**Patterns:**
- Application services throw typed exceptions for business failures: `ArgumentException`, `KeyNotFoundException`, `InvalidOperationException`, `UnauthorizedAccessException`, and custom `ForbiddenException` in `src/Woody.Application/Services/DirectMessagingService.cs`.
- API global error mapping is centralized in `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs`: unauthorized -> 401, forbidden -> 403, invalid operation -> 409, missing key -> 404, argument -> 400, unhandled -> 500.
- Some controllers still map exceptions locally, especially `src/Woody.Api/Controllers/BillingController.cs`; match the local controller pattern when editing an existing endpoint.
- Controller guard clauses return HTTP results directly for request parsing and authorization checks: `BadRequest()`, `Unauthorized()`, `NotFound()`, `Forbid()` in `src/Woody.Api/Controllers/PostsController.cs` and `src/Woody.Api/Controllers/CommunitiesController.cs`.
- Prefer not to leak internal exception messages for unhandled errors; the middleware logs unhandled exceptions and returns a generic response.

## Validation

**Patterns:**
- Controllers perform route/body parsing and basic request validation close to endpoints, for example `PostsController.Create` in `src/Woody.Api/Controllers/PostsController.cs`.
- Reusable validation helpers return a nullable error string, as in `CreateCommunityRequestValidator.Validate` in `src/Woody.Application/Utilities/CreateCommunityRequestValidator.cs`.
- Validation helpers also normalize data when reused by multiple callers, for example `CreateCommunityRequestValidator.NormalizeTags`.
- Domain invariants belong in domain policies, not controllers. Example: direct-message participant ordering and send permissions are centralized in `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`.
- Configuration validation occurs at startup in `src/Woody.Api/Program.cs`; missing critical options throw `InvalidOperationException`.

## Logging

**Framework:** `Microsoft.Extensions.Logging`

**Patterns:**
- Inject `ILogger<T>` into infrastructure/API classes that handle external boundaries or background-like processing: `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs`, `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`, `src/Woody.Api/Realtime/DirectMessageRealtimePublisher.cs`.
- Log unexpected exceptions at boundary layers. The global middleware logs unhandled API exceptions; services mostly communicate expected failures via exceptions instead of logging.
- `Console.*` logging was not detected in `src/**/*.cs`.

## Comments

**When to Comment:**
- Use comments for API/security behavior and domain invariants that are easy to break, for example SignalR JWT handling in `src/Woody.Api/Program.cs`.
- Use XML summary comments for domain policy rules and endpoint behavior where they document business semantics, as in `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs` and `src/Woody.Api/Controllers/ConversationsController.cs`.
- Avoid comments that restate simple assignments.

**JSDoc/TSDoc:**
- Not applicable. This is a C# backend.
- Use XML documentation comments (`/// <summary>`) where behavior is part of the domain contract.

## Function Design

**Size:** 
- Keep domain policy methods small and pure, as in `src/Woody.Domain/Messaging/DirectMessageConversationPolicy.cs`.
- Controllers in `src/Woody.Api/Controllers/PostsController.cs` and `src/Woody.Api/Controllers/CommunitiesController.cs` are large; new shared behavior should be extracted into application services, mapping helpers, validators, or domain policies instead of growing controllers further.

**Parameters:** 
- Pass `CancellationToken` through async API, service, and repository methods.
- Use typed route constraints where possible (`{conversationId:int}` in `src/Woody.Api/Controllers/ConversationsController.cs`) rather than string parsing for new endpoints.
- Use request DTOs for body payloads and `[FromBody]`/`[FromQuery]` attributes in controllers.

**Return Values:** 
- Controllers return `ActionResult<T>` for data endpoints and `IActionResult` for command endpoints.
- Services return DTOs or domain result objects; they throw typed exceptions for expected failure states.
- Repository read methods often use `AsNoTracking()` and return DTO-friendly collections/dictionaries.

## Module Design

**Exports:** 
- C# namespaces and project references define module boundaries.
- `src/Woody.Api` owns HTTP, SignalR, middleware, configuration, and dependency resolution.
- `src/Woody.Application` owns DTOs, service interfaces, use-case handlers, mapping, and application services.
- `src/Woody.Domain` owns entities, enums, policies, and entitlement rules.
- `src/Woody.Infrastructure` owns EF Core, repositories, migrations, external providers, and persistence configuration.

**Barrel Files:** 
- Not applicable. There are no TypeScript-style barrel exports.
- Static mapper/helper classes act as local aggregation points: `src/Woody.Application/Mapping/EntityMappers.cs`, `src/Woody.Application/Mapping/ConversationDtoMapper.cs`.

---

*Convention analysis: Monday Apr 27, 2026*
