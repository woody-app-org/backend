# Codebase Concerns

**Analysis Date:** Monday Apr 27, 2026

## Tech Debt

**[High] Public auth and verification endpoints have no application-level throttling:**
- Issue: Login, registration, e-mail verification code sending/resending, and code confirmation are public endpoints with no detected rate limiting, CAPTCHA, IP throttling, account lockout, or resend cooldown.
- Files: `src/Woody.Api/Controllers/AuthController.cs`, `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs`, `src/Woody.Application/Services/EmailVerificationService.cs`, `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs`
- Impact: Brute-force login attempts, verification-code guessing, e-mail enumeration, and outbound e-mail abuse can scale until infrastructure or Resend limits intervene. `EmailVerificationService` limits wrong code attempts per generated code, but does not limit code generation or login attempts.
- Fix approach: Add ASP.NET Core rate limiting in `src/Woody.Api/Program.cs` with endpoint-specific policies, add resend cooldown and daily quotas in `src/Woody.Application/Services/EmailVerificationService.cs`, and consider per-account/IP login failure tracking before issuing JWTs.

**[High] Feed ranking loads all visible candidates before pagination:**
- Issue: `FeedService.GetFeedAsync` retrieves every visible candidate, then filters, ranks, counts interactions, and paginates in memory.
- Files: `src/Woody.Application/Services/FeedService.cs`, `src/Woody.Infrastructure/Repositories/PostRepository.cs`
- Impact: Feed latency and memory usage grow with total visible posts. A single request can touch a large portion of `posts`, `likes`, `comments`, `follows`, `community_memberships`, and boosts before returning one page.
- Fix approach: Move ranking/windowing into repository queries or introduce a denormalized feed/materialized view. Keep page-size clamps in controllers/services, but avoid loading the complete candidate set for each request.

**[Medium] Controllers contain duplicated authorization and mutation orchestration:**
- Issue: Controllers parse string route IDs, check `User.GetUserId()`, call permission services, mutate EF entities, update counters, and map errors inline.
- Files: `src/Woody.Api/Controllers/CommunitiesController.cs`, `src/Woody.Api/Controllers/PostsController.cs`, `src/Woody.Api/Controllers/UsersController.cs`, `src/Woody.Api/Controllers/JoinRequestsController.cs`
- Impact: Permission and state-transition rules are easy to drift between endpoints. Membership status/role strings and counter updates are especially sensitive to partial updates or missing tests.
- Fix approach: Keep controllers thin. Move community membership mutations, post mutations, and profile updates into Application services/use cases with typed result objects and centralized validation.

**[Medium] Stringly typed roles/status/visibility are spread across API, application, and infrastructure:**
- Issue: Values such as `"owner"`, `"admin"`, `"member"`, `"active"`, `"pending"`, and `"public"` are compared as raw strings in multiple layers.
- Files: `src/Woody.Api/Controllers/CommunitiesController.cs`, `src/Woody.Application/Services/CommunityPermissionService.cs`, `src/Woody.Infrastructure/Repositories/CommunityMembershipRepository.cs`, `src/Woody.Infrastructure/Repositories/PostRepository.cs`
- Impact: Typos or inconsistent casing can bypass expected behavior or produce invisible data-quality bugs. `PatchMember` accepts role/status strings from request body and stores trimmed values directly.
- Fix approach: Introduce enums/value objects for membership role, membership status, and community visibility, then validate request DTOs before persistence.

**[Medium] Stripe webhook validation has duplicate/unused abstractions:**
- Issue: `StripeBillingWebhookProcessor` validates `Stripe-Signature` directly with `EventUtility.ConstructEvent`, while `IBillingWebhookSignatureVerifier`/`StripeBillingWebhookSignatureVerifier` is registered separately and not used by the webhook controller path.
- Files: `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`, `src/Woody.Infrastructure/Billing/Stripe/StripeBillingWebhookSignatureVerifier.cs`, `src/Woody.Api/Configuration/DependencyInjectionConfig.cs`, `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`
- Impact: Two signature-verification paths can diverge, and tests may cover the unused abstraction instead of the production path.
- Fix approach: Use a single verifier path from `StripeBillingWebhooksController` into the processor, or remove the unused abstraction and test the processor/controller path directly.

**[Low] Placeholder DTO fields leak unfinished API contract shape:**
- Issue: `Suggestions` is modeled as `List<object>` and currently mapped to an empty list.
- Files: `src/Woody.Application/DTOs/Api/UserProfileDto.cs`, `src/Woody.Application/Mapping/EntityMappers.cs`
- Impact: Clients may depend on a field with no stable schema. Future implementation can become a breaking change.
- Fix approach: Remove the field until implemented or replace it with a typed DTO and tests for the response contract.

## Known Bugs

**[Medium] Promoting arbitrary community roles/status through PATCH is possible for moderators:**
- Symptoms: A moderator can submit any non-empty `Role` or `Status` string and the API stores it without allowlist validation.
- Files: `src/Woody.Api/Controllers/CommunitiesController.cs`
- Trigger: Authenticated moderator calls `PATCH /api/communities/{communityId}/members/{userId}` with unsupported `role` or `status` text.
- Workaround: Use only known values from clients. Server-side allowlists are not detected.

**[Medium] E-mail verification consumed code can authorize a later registration for the same e-mail:**
- Symptoms: Registration checks for any consumed code for the normalized e-mail, not a specific pending registration request or recent code.
- Files: `src/Woody.Application/UseCases/Auth/Register/RegisterHandler.cs`, `src/Woody.Infrastructure/Repositories/EmailVerificationCodeRepository.cs`, `src/Woody.Application/Services/EmailVerificationService.cs`
- Trigger: A consumed verification row remains for an e-mail and a registration is attempted later for that same e-mail before a user exists.
- Workaround: Not detected. Add freshness, request binding, or consume-and-register transaction semantics.

**[Low] Password hash upgrade path is not handled:**
- Symptoms: `PasswordHasher.VerifyPassword` treats only `PasswordVerificationResult.Success` as valid and ignores `SuccessRehashNeeded`.
- Files: `src/Woody.Infrastructure/Security/PasswordHasher.cs`, `src/Woody.Application/UseCases/Auth/Login/LoginHandler.cs`
- Trigger: ASP.NET Identity changes hash parameters or legacy hashes need rehashing.
- Workaround: Login still succeeds only for hashes returning `Success`; no automatic rehash is detected.

## Security Considerations

**[High] Development seed can run outside Development via environment flag:**
- Risk: Demo users and known development credentials can be inserted into any environment where `WOODY_ENABLE_DEV_SEED=true`.
- Files: `src/Woody.Api/Program.cs`, `src/Woody.Infrastructure/Persistence/Seed/DbSeeder.cs`, `README.md`
- Current mitigation: The seed runs automatically only in Development; non-development requires the explicit `WOODY_ENABLE_DEV_SEED` flag.
- Recommendations: Add a second production guard, fail when `WOODY_ENABLE_DEV_SEED=true` and `ASPNETCORE_ENVIRONMENT=Production`, and keep documented seed credentials out of production-like databases.

**[High] Reverse proxy trust is wide open:**
- Risk: `ForwardedHeadersOptions` clears `KnownIPNetworks` and `KnownProxies`, so forwarded headers can be trusted from any source reaching the app.
- Files: `src/Woody.Api/Program.cs`
- Current mitigation: HSTS and HTTPS redirection are enabled outside Development, but forwarded-header trust boundaries are not constrained in code.
- Recommendations: Configure explicit proxy networks/proxies for the deployment platform or gate the broad trust behavior behind a Railway-specific environment check.

**[High] Public endpoints do not use rate limiting:**
- Risk: Authentication, e-mail verification, search, feed, public profile, public post, and Stripe webhook endpoints can be called repeatedly without detected server-side throttles.
- Files: `src/Woody.Api/Program.cs`, `src/Woody.Api/Controllers/AuthController.cs`, `src/Woody.Api/Controllers/SearchController.cs`, `src/Woody.Api/Controllers/FeedController.cs`, `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`
- Current mitigation: JWT is required for mutations; Stripe webhook signatures are verified; e-mail verification has per-code attempt count.
- Recommendations: Add `AddRateLimiter`/`UseRateLimiter`, separate stricter policies for auth/e-mail/webhook endpoints, and return consistent 429 responses.

**[Medium] Sensitive local configuration files are present:**
- Risk: `.env`, `.env.example`, and `docker-compose.yml` exist in the repo root. `.env` must never be committed or quoted. `docker-compose.yml` may include local credentials.
- Files: `.env`, `.env.example`, `docker-compose.yml`, `.gitignore`
- Current mitigation: `.gitignore` is modified in the working tree and likely intended to protect local env files; contents were not read during this audit.
- Recommendations: Keep `.env` ignored, audit `.gitignore` before committing, avoid documenting real secrets, and rotate any secret accidentally committed.

**[Medium] Error responses expose domain exception messages directly:**
- Risk: `ExceptionHandlingMiddleware` serializes exception messages for `UnauthorizedAccessException`, `InvalidOperationException`, `ArgumentException`, and other expected errors.
- Files: `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs`, `src/Woody.Api/Controllers/BillingController.cs`
- Current mitigation: Unexpected exceptions are logged and replaced with a generic message.
- Recommendations: Use typed application errors with public-safe messages and avoid leaking provider, account, or state details from exception text.

**[Low] CORS development policy reflects all origins with credentials:**
- Risk: In Development, missing `CORS_ORIGINS` enables `SetIsOriginAllowed(_ => true)` with credentials.
- Files: `src/Woody.Api/Program.cs`
- Current mitigation: The permissive branch is limited to Development; production has no CORS unless `CORS_ORIGINS` is set.
- Recommendations: Keep this branch out of shared/staging environments and set explicit origins anywhere real user data exists.

## Performance Bottlenecks

**[High] Feed candidate scan and in-memory ranking:**
- Problem: Feed requests fetch all visible post candidates and then do in-memory ordering and slicing.
- Files: `src/Woody.Application/Services/FeedService.cs`, `src/Woody.Infrastructure/Repositories/PostRepository.cs`
- Cause: `ListNonDeletedVisibleFeedCandidatesAsync` returns all candidates for the viewer before `Skip`/`Take` is applied in application code.
- Improvement path: Push filtering/ranking/pagination into SQL, cache ranking inputs, or precompute feed pages.

**[Medium] Community analytics uses bounded scans and client-side grouping:**
- Problem: Top-post analytics scans the newest 400 posts, then groups likes/comments/tags in memory for some calculations.
- Files: `src/Woody.Infrastructure/Repositories/CommunityAnalyticsReadRepository.cs`, `src/Woody.Application/Services/CommunityDashboardAnalyticsService.cs`
- Cause: `scanCap = 400` protects the query but can omit older high-engagement posts in the requested period and still performs multiple round trips.
- Improvement path: Use SQL aggregation by score across the requested range, persist daily rollups for more metrics, and document the cap if intentional.

**[Medium] Repository search uses lower-case contains on columns:**
- Problem: Search calls `ToLower().Contains(...)` over post/community title/content/name/slug/description fields.
- Files: `src/Woody.Infrastructure/Repositories/PostRepository.cs`, `src/Woody.Infrastructure/Repositories/CommunityRepository.cs`
- Cause: Case normalization is performed in query expressions instead of using database-native text search/indexing.
- Improvement path: Add PostgreSQL full-text search or trigram indexes for user-facing search.

## Fragile Areas

**Billing and subscription synchronization:**
- Files: `src/Woody.Infrastructure/Billing/StripePayments/StripeBillingWebhookProcessor.cs`, `src/Woody.Infrastructure/Repositories/BillingWebhookReceiptRepository.cs`, `src/Woody.Application/Billing/*`, `src/Woody.Api/Controllers/StripeBillingWebhooksController.cs`
- Why fragile: Webhook processing updates user and community subscription state based on provider metadata, event order, idempotency receipts, and fallback lookups. Invalid payloads release the receipt claim, while duplicate deliveries are acknowledged.
- Safe modification: Add integration tests around each Stripe event type before changing behavior. Verify duplicate delivery, transient failure, missing metadata, user subscription, and community subscription flows.
- Test coverage: No Stripe webhook tests detected in `tests/Woody.Api.Tests`, `tests/Woody.Application.Tests`, or `tests/Woody.Infrastructure.Tests`.

**Community membership and moderation:**
- Files: `src/Woody.Api/Controllers/CommunitiesController.cs`, `src/Woody.Api/Controllers/JoinRequestsController.cs`, `src/Woody.Application/Services/CommunityPermissionService.cs`, `src/Woody.Infrastructure/Repositories/CommunityMembershipRepository.cs`
- Why fragile: Role/status strings drive authorization and member counts. Several endpoints update memberships and counters in separate saves.
- Safe modification: Add unit/integration tests for owner/admin/member transitions, invalid role/status rejection, owner removal, leave/remove flows, and member-count consistency.
- Test coverage: No API or application tests detected for community membership mutations.

**Post visibility and private community access:**
- Files: `src/Woody.Api/Controllers/PostsController.cs`, `src/Woody.Infrastructure/Repositories/PostRepository.cs`, `src/Woody.Application/Services/FeedService.cs`
- Why fragile: Visibility logic is split between controller helper methods, repository predicates, and feed ranking. Private community posts must stay hidden from non-members across detail, comments, profile posts, and feed.
- Safe modification: Add cross-endpoint tests with anonymous viewer, author, member, non-member, public community, and private community cases.
- Test coverage: No API tests detected for private community visibility.

**Design-time and runtime database configuration:**
- Files: `src/Woody.Infrastructure/Persistence/Configuration/DatabaseConnectionResolver.cs`, `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbConfiguration.cs`, `src/Woody.Infrastructure/Persistence/Configuration/WoodyDbContextFactory.cs`
- Why fragile: Runtime sensitive logging is Development-only, but design-time factory always enables sensitive data logging. `DATABASE_URL` conversion applies only recognized query parameters.
- Safe modification: Avoid logging generated connection strings, keep EF tooling local, and test `DATABASE_URL` conversion before deployment-platform changes.
- Test coverage: No detected tests for database URL parsing or configuration.

## Scaling Limits

**Feed size:**
- Current capacity: Page size is clamped to 50, but the candidate set is unbounded before pagination.
- Limit: Large post volumes or users with broad visibility will degrade request time and memory.
- Scaling path: Query-level pagination/ranking, caching, and precomputed ranking tables.

**Analytics windows:**
- Current capacity: Top posts scan the newest 400 posts in the range.
- Limit: Communities with more than 400 posts in the selected period can produce incomplete top-post analytics.
- Scaling path: SQL aggregation and rollups for all high-volume metrics.

**SignalR/JWT via query string:**
- Current capacity: Hub auth accepts `access_token` query parameter for `/hubs/...`.
- Limit: Query-string tokens can appear in proxy/server logs depending on hosting configuration.
- Scaling path: Ensure access logs redact query strings, keep hub route restricted, and prefer short-lived tokens.

## Dependencies at Risk

**Targeting .NET 10.0:**
- Risk: Projects target `net10.0`, which may constrain build agents and hosting environments to a current/preview SDK/runtime depending on deployment date.
- Impact: CI/CD and production runtime mismatches can block builds or deployments.
- Migration plan: Pin the SDK with `global.json` or align target frameworks with the deployed runtime.

**Moq and EF InMemory test setup without actual infrastructure tests:**
- Risk: Test dependencies exist, but no infrastructure tests were detected in `tests/Woody.Infrastructure.Tests`.
- Impact: EF translation, PostgreSQL constraints, migrations, and repository behavior can regress unnoticed.
- Migration plan: Add repository tests using PostgreSQL/Testcontainers or SQLite where appropriate; avoid relying only on EF InMemory for relational behavior.

## Missing Critical Features

**Rate limiting and abuse controls:**
- Problem: Not detected for public endpoints, especially auth and e-mail verification.
- Blocks: Safer production launch and resilience against brute-force/spam behavior.

**Centralized input validation:**
- Problem: Validation is mostly inline in controllers/services, with some request fields stored after trimming but without robust allowlists or length/url validation.
- Blocks: Consistent API errors and protection against malformed data in community roles/status, URLs, post images, profile links, and search strings.

**Token revocation/logout semantics:**
- Problem: Logout endpoint returns `204 NoContent`, but no denylist, refresh token store, or token revocation mechanism was detected.
- Blocks: Server-side session invalidation after logout, credential compromise, or role/plan changes before JWT expiry.

**API documentation beyond README:**
- Problem: README documents setup, but endpoint-level API contract documentation, security model, and operational runbook were not detected.
- Blocks: Safe client integration and production operations for auth, billing webhooks, communities, posts, and direct messaging.

## Test Coverage Gaps

**API layer:**
- What's not tested: Controllers, authentication, authorization, CORS/proxy behavior, health checks, Stripe webhook HTTP responses, and private-community visibility.
- Files: `tests/Woody.Api.Tests/UnitTest1.cs`, `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`, `src/Woody.Api/Controllers/*`
- Risk: Route attributes, authorization attributes, exception mapping, and response codes can regress unnoticed.
- Priority: High

**Application layer:**
- What's not tested: Login/register, e-mail verification, billing checkout handlers, community premium entitlement service, feed service, post enrichment, direct messaging service, and membership permissions.
- Files: `tests/Woody.Application.Tests/UnitTest1.cs`, `tests/Woody.Application.Tests/Woody.Application.Tests.csproj`, `src/Woody.Application/*`
- Risk: Business rules can change without failing tests.
- Priority: High

**Infrastructure layer:**
- What's not tested: Repositories, database URL parsing, EF mappings, migrations, Stripe gateway/processor, Resend sender, and billing receipt idempotency.
- Files: `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`, `src/Woody.Infrastructure/*`
- Risk: SQL translation and external provider integration can fail only at runtime.
- Priority: High

**Domain layer:**
- What's not tested: Some direct-message and pinning policies are tested, but subscription entitlement, community premium gates, post publication constraints, and entity invariants have limited/no detected coverage.
- Files: `tests/Woody.Domain.Tests/*`, `src/Woody.Domain/*`
- Risk: Core policy changes can break billing/community behavior without coverage.
- Priority: Medium

---

*Concerns audit: Monday Apr 27, 2026*
