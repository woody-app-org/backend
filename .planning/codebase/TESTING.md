# Testing Patterns

**Analysis Date:** Monday Apr 27, 2026

## Test Framework

**Runner:**
- xUnit `2.9.3`
- Config: test project files under `tests/*/*.csproj`
- Projects:
  - `tests/Woody.Domain.Tests/Woody.Domain.Tests.csproj`
  - `tests/Woody.Application.Tests/Woody.Application.Tests.csproj`
  - `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`
  - `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`

**Assertion Library:**
- Primary: xUnit `Assert.*`, visible in `tests/Woody.Domain.Tests/DirectMessageConversationPolicyTests.cs`.
- Additional package: FluentAssertions `8.8.0` is referenced only by `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`; no current usage was detected in test source files.
- Mocking package: Moq `4.20.72` is referenced by `tests/Woody.Application.Tests/Woody.Application.Tests.csproj` and `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`; no current usage was detected in test source files.

**Run Commands:**
```bash
dotnet restore                         # Restore all solution projects
dotnet build --no-restore              # Build after restore
dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"  # CI test command
dotnet test                            # Local all-tests command
dotnet test --collect:"XPlat Code Coverage"  # Coverage collection via coverlet.collector where referenced
```

## Test File Organization

**Location:**
- Tests are separated under `tests/`, grouped by production layer.
- Test projects mirror production projects from `src/`: `Woody.Domain.Tests`, `Woody.Application.Tests`, `Woody.Infrastructure.Tests`, and `Woody.Api.Tests`.
- The solution file `Woody.sln` includes all four production projects and all four test projects.

**Naming:**
- Test files end with `Tests.cs`, for example `tests/Woody.Domain.Tests/PostCommentPinPolicyTests.cs`.
- Test classes usually end with `Tests`; some are `sealed`, for example `PostCommentPinPolicyTests`.
- Test methods use behavior-oriented snake_case names, for example `OrderParticipantPair_normalizes_low_high` and `CanPin_root_visible_comment_by_post_author`.
- Placeholder files named `UnitTest1.cs` exist in `tests/Woody.Api.Tests/UnitTest1.cs`, `tests/Woody.Application.Tests/UnitTest1.cs`, and `tests/Woody.Domain.Tests/UnitTest1.cs`.

**Structure:**
```text
tests/
├── Woody.Api.Tests/
│   ├── Woody.Api.Tests.csproj
│   └── UnitTest1.cs
├── Woody.Application.Tests/
│   ├── Woody.Application.Tests.csproj
│   └── UnitTest1.cs
├── Woody.Domain.Tests/
│   ├── Woody.Domain.Tests.csproj
│   ├── DirectMessageAttachmentPolicyTests.cs
│   ├── DirectMessageConversationPolicyTests.cs
│   ├── DirectMessageMessagePolicyTests.cs
│   ├── PostCommentPinPolicyTests.cs
│   ├── PostProfilePinPolicyTests.cs
│   └── UnitTest1.cs
└── Woody.Infrastructure.Tests/
    └── Woody.Infrastructure.Tests.csproj
```

## Test Structure

**Suite Organization:**
```csharp
public class DirectMessageConversationPolicyTests
{
    [Fact]
    public void OrderParticipantPair_normalizes_low_high()
    {
        Assert.Equal((1, 9), DirectMessageConversationPolicy.OrderParticipantPair(9, 1));
        Assert.Equal((1, 9), DirectMessageConversationPolicy.OrderParticipantPair(1, 9));
    }
}
```

**Patterns:**
- Unit tests instantiate domain entities directly, as in `tests/Woody.Domain.Tests/PostCommentPinPolicyTests.cs`.
- Domain tests call pure policy methods and assert booleans, exceptions, or enum outputs.
- `[Theory]` plus `[InlineData]` is used for compact input matrices, as in `tests/Woody.Domain.Tests/DirectMessageAttachmentPolicyTests.cs`.
- No common setup/teardown fixtures were detected.
- No test base classes were detected.

## Mocking

**Framework:** Moq is referenced, but no current `Mock<T>` usage was detected.

**Patterns:**
```csharp
// Current tests mostly avoid mocks by targeting pure domain policies.
var post = new Post { Id = 1, UserId = 10, DeletedAt = null };
var comment = new Comment { PostId = 1, ParentCommentId = null, DeletedAt = null };
Assert.True(PostCommentPinPolicy.CanPinCommentOnPost(10, post, comment));
```

**What to Mock:**
- Mock application interfaces from `src/Woody.Application/Interfaces/` when testing services in `tests/Woody.Application.Tests`.
- Mock external gateways and senders, such as billing or email interfaces, rather than Stripe/Resend SDK clients.
- Prefer EF Core InMemory for repository/persistence tests because `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj` references `Microsoft.EntityFrameworkCore.InMemory`.

**What NOT to Mock:**
- Do not mock domain policy classes such as `DirectMessageConversationPolicy`; call them directly.
- Do not mock simple domain entities; instantiate them inline.
- Do not mock DTO mappers unless testing a higher-level service where mapping is outside the behavior under test.

## Fixtures and Factories

**Test Data:**
```csharp
var c = new Conversation
{
    UserLowId = 1,
    UserHighId = 2,
    InitiatorUserId = 2,
    Status = ConversationStatus.Pending
};
```

**Location:**
- Test data is currently inline inside each test method.
- No fixture directory, factory classes, AutoFixture, builder pattern, or shared test data helpers were detected.
- Add factories under the relevant test project only when repeated setup becomes noisy, for example `tests/Woody.Domain.Tests/TestData/`.

## Coverage

**Requirements:** None enforced.

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Observed Coverage Tooling:**
- `coverlet.collector` `6.0.4` is referenced by `tests/Woody.Domain.Tests/Woody.Domain.Tests.csproj`, `tests/Woody.Application.Tests/Woody.Application.Tests.csproj`, and `tests/Woody.Api.Tests/Woody.Api.Tests.csproj`.
- `coverlet.collector` is not referenced by `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj`.
- No coverage threshold, `.runsettings`, coverage report publishing, or coverage gate was detected.

## Test Types

**Unit Tests:**
- Present and concentrated in `tests/Woody.Domain.Tests`.
- Best-covered observable area is domain policy behavior: direct-message policies and post/comment pin policies.
- Use `[Fact]`, `[Theory]`, direct entity construction, and xUnit assertions.

**Integration Tests:**
- Infrastructure and API test projects exist, but meaningful integration tests were not detected.
- `tests/Woody.Infrastructure.Tests/Woody.Infrastructure.Tests.csproj` references EF Core InMemory, FluentAssertions, and Moq, but no `.cs` test files were detected in that project.
- `tests/Woody.Api.Tests/Woody.Api.Tests.csproj` references `Microsoft.AspNetCore.Mvc.Testing`, but only a blank placeholder `UnitTest1` was detected.

**E2E Tests:**
- Not used in the backend repository.
- No Playwright, Selenium, Newman, or end-to-end API suite was detected.

## Common Patterns

**Async Testing:**
```csharp
// No async test pattern was detected in current test source files.
// Use async Task test methods for services/controllers when adding coverage.
[Fact]
public async Task ServiceMethod_expected_behavior()
{
    // arrange
    // act
    // assert
}
```

**Error Testing:**
```csharp
[Fact]
public void OrderParticipantPair_rejects_same_user()
{
    Assert.Throws<ArgumentException>(() => DirectMessageConversationPolicy.OrderParticipantPair(3, 3));
}
```

## CI Verification

**Pipeline:**
- GitHub Actions workflow: `.github/workflows/dotnet.yml`.
- The workflow runs on `push` and `pull_request`.
- Steps: checkout, setup .NET `10.0.x`, `dotnet restore`, `dotnet build --no-restore`, and `dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"`.

**Local Verification Guidance:**
- Run from repo root: `dotnet test`.
- For CI parity after a fresh restore: `dotnet restore`, `dotnet build --no-restore`, then `dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"`.
- For database-related local development, `README.md` documents PostgreSQL setup and EF migration commands, but current tests do not require a running PostgreSQL database.

## Gaps and Cautions

- `tests/Woody.Api.Tests/UnitTest1.cs`, `tests/Woody.Application.Tests/UnitTest1.cs`, and `tests/Woody.Domain.Tests/UnitTest1.cs` are blank placeholder tests and should not be treated as behavior coverage.
- Controller behavior in `src/Woody.Api/Controllers/PostsController.cs`, `src/Woody.Api/Controllers/CommunitiesController.cs`, `src/Woody.Api/Controllers/ConversationsController.cs`, and middleware behavior in `src/Woody.Api/Middlewares/ExceptionHandlingMiddleware.cs` lack observable tests.
- Application services such as `src/Woody.Application/Services/DirectMessagingService.cs`, `src/Woody.Application/Services/FeedService.cs`, and `src/Woody.Application/Services/EmailVerificationService.cs` lack detected test coverage.
- Infrastructure repositories and EF mapping behavior lack detected tests despite an infrastructure test project.
- No automated lint/style command is configured; `dotnet build` is the effective non-test verification gate.

---

*Testing analysis: Monday Apr 27, 2026*
