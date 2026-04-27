using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;

namespace Woody.Infrastructure.Tests;

public class RefreshTokenSessionRepositoryTests
{
    [Fact]
    public async Task GetByTokenHashAsync_ReturnsPersistedSession()
    {
        await using var db = CreateDbContext();
        var repo = new RefreshTokenSessionRepository(db);
        var session = new RefreshTokenSession
        {
            UserId = 1,
            TokenHash = "abc123",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await repo.AddAsync(session);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByTokenHashAsync("abc123");

        Assert.NotNull(loaded);
        Assert.Equal(1, loaded.UserId);
    }

    [Fact]
    public async Task RevokeActiveForUserAsync_RevokesOnlyActiveUserSessions()
    {
        await using var db = CreateDbContext();
        var repo = new RefreshTokenSessionRepository(db);
        var now = DateTime.UtcNow;
        await repo.AddAsync(new RefreshTokenSession
        {
            UserId = 1,
            TokenHash = "active",
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        });
        await repo.AddAsync(new RefreshTokenSession
        {
            UserId = 1,
            TokenHash = "expired",
            CreatedAt = now.AddDays(-2),
            ExpiresAt = now.AddDays(-1)
        });
        await repo.AddAsync(new RefreshTokenSession
        {
            UserId = 2,
            TokenHash = "other-user",
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        });
        await repo.SaveChangesAsync();

        await repo.RevokeActiveForUserAsync(1, now, "password_changed");
        await repo.SaveChangesAsync();

        Assert.NotNull((await repo.GetByTokenHashAsync("active"))!.RevokedAt);
        Assert.Null((await repo.GetByTokenHashAsync("expired"))!.RevokedAt);
        Assert.Null((await repo.GetByTokenHashAsync("other-user"))!.RevokedAt);
    }

    private static WoodyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoodyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WoodyDbContext(options);
    }
}
