using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Persistence.Seed;

namespace Woody.Infrastructure.Tests;

public class BadgeSeedTests
{
    [Fact]
    public void DbSeeder_SeedBadges_IsIdempotentAndCreatesSeedDefinition()
    {
        var options = new DbContextOptionsBuilder<WoodyDbContext>()
            .UseInMemoryDatabase($"badge-seed-{Guid.NewGuid():N}")
            .Options;

        using (var db = new WoodyDbContext(options))
        {
            DbSeeder.Seed(db);
        }

        using (var assertDb = new WoodyDbContext(options))
        {
            var countAfterFirst = assertDb.Badges.Count(b => b.Slug == "seed");
            Assert.Equal(1, countAfterFirst);

            DbSeeder.Seed(assertDb);

            var countAfterSecond = assertDb.Badges.Count(b => b.Slug == "seed");
            Assert.Equal(1, countAfterSecond);

            var seed = assertDb.Badges.Single(b => b.Slug == "seed");
            Assert.Equal("Seed", seed.Name);
            Assert.Equal("Presente desde o primeiro dia da Woody.", seed.Description);
            Assert.Equal("seed", seed.IconAssetKey);
            Assert.Equal("founding", seed.Category);
            Assert.Equal("founder", seed.Rarity);
            Assert.True(seed.IsActive);
            Assert.Equal(10, seed.SortOrder);
        }
    }
}
