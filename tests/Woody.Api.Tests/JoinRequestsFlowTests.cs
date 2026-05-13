using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woody.Application.Billing;
using Woody.Application.DTOs;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;

namespace Woody.Api.Tests;

public class JoinRequestsFlowTests
{
    private const string Password = "CorrectHorseBatteryStaple!1";

    private static Dictionary<string, string?> JoinRequestsTestHostSettings() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=woody_tests;Username=postgres;Password=postgres",
        ["Jwt:Secret"] = "test-secret-that-is-at-least-32-chars",
        ["Jwt:Issuer"] = "Woody.Api.Tests",
        ["Jwt:Audience"] = "Woody.Api.Tests",
        ["Jwt:ExpirationMinutes"] = "15",
        ["Resend:ApiKey"] = "test-resend-key",
        ["Resend:FromEmail"] = "no-reply@example.com",
        ["EmailVerification:ExpirationMinutes"] = "10",
        ["EmailVerification:MaxAttempts"] = "5",
        ["AuthSecurity:MaxFailedLoginAttempts"] = "5",
        ["AuthSecurity:LockoutMinutes"] = "15"
    };

    [Fact]
    public async Task RequestJoin_private_then_me_returns_pending()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var bobToken = await factory.LoginAsync(client, JoinRequestsApiFactory.BobUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);

        var r1 = await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);
        Assert.Equal(HttpStatusCode.NoContent, r1.StatusCode);

        var me = await client.GetFromJsonAsync<JsonElement>($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me");
        Assert.Equal("pending", me.GetProperty("status").GetString());
        Assert.True(me.GetProperty("canRequest").GetBoolean() == false);
        Assert.False(string.IsNullOrEmpty(me.GetProperty("requestId").GetString()));
    }

    [Fact]
    public async Task RequestJoin_second_call_is_idempotent()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var bobToken = await factory.LoginAsync(client, JoinRequestsApiFactory.BobUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null)).StatusCode);
    }

    [Fact]
    public async Task RequestJoin_when_already_active_member_returns_no_content_without_new_request()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var aliceToken = await factory.LoginAsync(client, JoinRequestsApiFactory.AliceUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null)).StatusCode);
    }

    [Fact]
    public async Task RequestJoin_when_banned_returns_403()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var daveToken = await factory.LoginAsync(client, JoinRequestsApiFactory.DaveUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", daveToken);

        var r = await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task JoinPublic_when_banned_returns_403()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var daveToken = await factory.LoginAsync(client, JoinRequestsApiFactory.DaveUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", daveToken);

        var r = await client.PostAsync($"/api/communities/{factory.PublicCommunityId}/members", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Reject_persists_reason_and_reviewer()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var bobToken = await factory.LoginAsync(client, JoinRequestsApiFactory.BobUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);
        await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);

        var me1 = await client.GetFromJsonAsync<JsonElement>($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me");
        var jrId = int.Parse(me1.GetProperty("requestId").GetString()!);

        var aliceClient = factory.CreateClient();
        var aliceToken = await factory.LoginAsync(aliceClient, JoinRequestsApiFactory.AliceUsername, Password);
        aliceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);

        var reject = await aliceClient.PostAsJsonAsync($"/api/join-requests/{jrId}/reject", new { reason = "  Fora do tema  " });
        Assert.Equal(HttpStatusCode.NoContent, reject.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        var jr = await db.JoinRequests.AsNoTracking().FirstAsync(j => j.Id == jrId);
        Assert.Equal("rejected", jr.Status);
        Assert.Equal("Fora do tema", jr.RejectionReason);
        Assert.NotNull(jr.ReviewedAt);
        Assert.Equal(factory.AliceUserId, jr.ReviewedByUserId);
    }

    [Fact]
    public async Task Approve_creates_active_membership_and_audit_fields()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var bobToken = await factory.LoginAsync(client, JoinRequestsApiFactory.BobUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);
        await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);

        var me1 = await client.GetFromJsonAsync<JsonElement>($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me");
        var jrId = int.Parse(me1.GetProperty("requestId").GetString()!);

        var aliceClient = factory.CreateClient();
        var aliceToken = await factory.LoginAsync(aliceClient, JoinRequestsApiFactory.AliceUsername, Password);
        aliceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await aliceClient.PostAsync($"/api/join-requests/{jrId}/approve", null)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        var jr = await db.JoinRequests.AsNoTracking().FirstAsync(j => j.Id == jrId);
        Assert.Equal("approved", jr.Status);
        Assert.NotNull(jr.ReviewedAt);
        Assert.Equal(factory.AliceUserId, jr.ReviewedByUserId);

        var m = await db.CommunityMemberships.AsNoTracking()
            .FirstAsync(x => x.UserId == factory.BobUserId && x.CommunityId == factory.PrivateCommunity1Id);
        Assert.Equal("active", m.Status);
    }

    [Fact]
    public async Task Bob_cannot_approve_own_join_request()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var bobClient = factory.CreateClient();
        var bobToken = await factory.LoginAsync(bobClient, JoinRequestsApiFactory.BobUsername, Password);
        bobClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);
        await bobClient.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);

        var me1 = await bobClient.GetFromJsonAsync<JsonElement>($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me");
        var jrId = int.Parse(me1.GetProperty("requestId").GetString()!);

        var r = await bobClient.PostAsync($"/api/join-requests/{jrId}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Carol_cannot_approve_join_request_for_alice_community()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var bobClient = factory.CreateClient();
        var bobToken = await factory.LoginAsync(bobClient, JoinRequestsApiFactory.BobUsername, Password);
        bobClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);
        await bobClient.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);

        var me1 = await bobClient.GetFromJsonAsync<JsonElement>($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me");
        var jrId = int.Parse(me1.GetProperty("requestId").GetString()!);

        var carolClient = factory.CreateClient();
        var carolToken = await factory.LoginAsync(carolClient, JoinRequestsApiFactory.CarolUsername, Password);
        carolClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", carolToken);

        var r = await carolClient.PostAsync($"/api/join-requests/{jrId}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Cancel_my_pending_then_me_allows_request_again()
    {
        await using var factory = new JoinRequestsApiFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var bobToken = await factory.LoginAsync(client, JoinRequestsApiFactory.BobUsername, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bobToken);
        await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests", null);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me/cancel", null)).StatusCode);

        var me = await client.GetFromJsonAsync<JsonElement>($"/api/communities/{factory.PrivateCommunity1Id}/join-requests/me");
        Assert.Equal("cancelled", me.GetProperty("status").GetString());
        Assert.True(me.GetProperty("canRequest").GetBoolean());
    }

    [Fact]
    public async Task Approve_when_applicant_membership_banned_returns_403_and_leaves_request_pending()
    {
        await using var factory = new BannedApplicantJoinRequestFactory();
        await factory.SeedAsync();
        var aliceClient = factory.CreateClient();
        var aliceToken = await factory.LoginAsync(aliceClient, BannedApplicantJoinRequestFactory.AliceUsername, Password);
        aliceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aliceToken);

        var r = await aliceClient.PostAsync($"/api/join-requests/{factory.PendingJoinRequestId}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
        var jr = await db.JoinRequests.AsNoTracking().FirstAsync(j => j.Id == factory.PendingJoinRequestId);
        Assert.Equal("pending", jr.Status);
        var m = await db.CommunityMemberships.AsNoTracking()
            .FirstAsync(x => x.UserId == factory.BobUserId && x.CommunityId == factory.PrivateCommunityId);
        Assert.Equal("banned", m.Status);
    }

    private sealed class JoinRequestsApiFactory : WebApplicationFactory<Program>
    {
        public const string AliceUsername = "join-alice";
        public const string BobUsername = "join-bob";
        public const string CarolUsername = "join-carol";
        public const string DaveUsername = "join-dave";

        private readonly string _databaseName = Guid.NewGuid().ToString();
        private bool _seeded;

        public int AliceUserId { get; private set; }
        public int BobUserId { get; private set; }
        public int PrivateCommunity1Id { get; private set; }
        public int PublicCommunityId { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            foreach (var pair in JoinRequestsTestHostSettings())
            {
                if (pair.Value != null)
                    builder.UseSetting(pair.Key, pair.Value);
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<WoodyDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<WoodyDbContext>>();
                services.AddDbContext<WoodyDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }

        public async Task SeedAsync()
        {
            if (_seeded)
                return;
            _seeded = true;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
            var hasher = new PasswordHasher();
            var now = DateTime.UtcNow;

            var alice = new User
            {
                Username = AliceUsername,
                Email = "join-alice@example.com",
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Alice",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now
            };
            var bob = new User
            {
                Username = BobUsername,
                Email = "join-bob@example.com",
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Bob",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now
            };
            var carol = new User
            {
                Username = CarolUsername,
                Email = "join-carol@example.com",
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Carol",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now
            };
            var dave = new User
            {
                Username = DaveUsername,
                Email = "join-dave@example.com",
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Dave",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.AddRange(alice, bob, carol, dave);
            await db.SaveChangesAsync();

            foreach (var u in new[] { alice, bob, carol, dave })
            {
                db.UserSubscriptions.Add(new UserSubscription
                {
                    UserId = u.Id,
                    Plan = SubscriptionPlan.Free,
                    Status = SubscriptionStatus.Active,
                    BillingProvider = BillingProvider.None,
                    PlanCode = "free",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await db.SaveChangesAsync();

            AliceUserId = alice.Id;
            BobUserId = bob.Id;

            var comm1 = new Community
            {
                Slug = "join-priv-one",
                Name = "Private One",
                Description = "Descrição com mais de dez caracteres.",
                Category = "outro",
                Rules = "Regras",
                Visibility = "private",
                OwnerUserId = alice.Id,
                MemberCount = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            var comm2 = new Community
            {
                Slug = "join-priv-two",
                Name = "Private Two",
                Description = "Descrição com mais de dez caracteres.",
                Category = "outro",
                Rules = "Regras",
                Visibility = "private",
                OwnerUserId = carol.Id,
                MemberCount = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            var commPub = new Community
            {
                Slug = "join-pub-ban",
                Name = "Public Ban Test",
                Description = "Descrição com mais de dez caracteres.",
                Category = "outro",
                Rules = "Regras",
                Visibility = "public",
                OwnerUserId = alice.Id,
                MemberCount = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Communities.AddRange(comm1, comm2, commPub);
            await db.SaveChangesAsync();

            PrivateCommunity1Id = comm1.Id;
            PublicCommunityId = commPub.Id;

            foreach (var c in new[] { comm1, comm2, commPub })
            {
                db.CommunitySubscriptions.Add(new CommunitySubscription
                {
                    CommunityId = c.Id,
                    Plan = CommunityPlan.Free,
                    Status = SubscriptionStatus.Active,
                    PlanCode = CommunityBillingPlanCodes.Free,
                    BillingProvider = BillingProvider.None,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = alice.Id,
                CommunityId = comm1.Id,
                Role = "owner",
                Status = "active",
                JoinedAt = now
            });
            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = carol.Id,
                CommunityId = comm2.Id,
                Role = "owner",
                Status = "active",
                JoinedAt = now
            });
            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = alice.Id,
                CommunityId = commPub.Id,
                Role = "owner",
                Status = "active",
                JoinedAt = now
            });
            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = dave.Id,
                CommunityId = commPub.Id,
                Role = "member",
                Status = "banned",
                JoinedAt = now
            });
            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = dave.Id,
                CommunityId = comm1.Id,
                Role = "member",
                Status = "banned",
                JoinedAt = now
            });

            await db.SaveChangesAsync();
        }

        public async Task<string> LoginAsync(HttpClient client, string username, string password)
        {
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
            {
                Username = username,
                Password = password
            });
            loginResponse.EnsureSuccessStatusCode();
            var login = await loginResponse.Content.ReadFromJsonAsync<LoginResultDTO>();
            Assert.False(string.IsNullOrWhiteSpace(login?.Token));
            return login!.Token;
        }
    }

    /// <summary>Bob está banida na comunidade privada mas existe pedido pendente legado (inconsistência) — aprovação não deve reativar.</summary>
    private sealed class BannedApplicantJoinRequestFactory : WebApplicationFactory<Program>
    {
        public const string AliceUsername = "banjr-alice";
        public const string BobUsername = "banjr-bob";

        private readonly string _databaseName = Guid.NewGuid().ToString();
        private bool _seeded;

        public int PendingJoinRequestId { get; private set; }
        public int PrivateCommunityId { get; private set; }
        public int BobUserId { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            foreach (var pair in JoinRequestsTestHostSettings())
            {
                if (pair.Value != null)
                    builder.UseSetting(pair.Key, pair.Value);
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<WoodyDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<WoodyDbContext>>();
                services.AddDbContext<WoodyDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }

        public async Task SeedAsync()
        {
            if (_seeded)
                return;
            _seeded = true;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WoodyDbContext>();
            var hasher = new PasswordHasher();
            var now = DateTime.UtcNow;

            var alice = new User
            {
                Username = AliceUsername,
                Email = "banjr-alice@example.com",
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Alice",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now
            };
            var bob = new User
            {
                Username = BobUsername,
                Email = "banjr-bob@example.com",
                Password = hasher.HashPassword(Password),
                Role = "User",
                DisplayName = "Bob",
                IsEmailVerified = true,
                VerificationStatus = VerificationStatus.Approved,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.AddRange(alice, bob);
            await db.SaveChangesAsync();

            foreach (var u in new[] { alice, bob })
            {
                db.UserSubscriptions.Add(new UserSubscription
                {
                    UserId = u.Id,
                    Plan = SubscriptionPlan.Free,
                    Status = SubscriptionStatus.Active,
                    BillingProvider = BillingProvider.None,
                    PlanCode = "free",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await db.SaveChangesAsync();

            BobUserId = bob.Id;

            var comm1 = new Community
            {
                Slug = "banjr-priv",
                Name = "Private Ban Jr",
                Description = "Descrição com mais de dez caracteres.",
                Category = "outro",
                Rules = "Regras",
                Visibility = "private",
                OwnerUserId = alice.Id,
                MemberCount = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Communities.Add(comm1);
            await db.SaveChangesAsync();
            PrivateCommunityId = comm1.Id;

            db.CommunitySubscriptions.Add(new CommunitySubscription
            {
                CommunityId = comm1.Id,
                Plan = CommunityPlan.Free,
                Status = SubscriptionStatus.Active,
                PlanCode = CommunityBillingPlanCodes.Free,
                BillingProvider = BillingProvider.None,
                CreatedAt = now,
                UpdatedAt = now
            });

            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = alice.Id,
                CommunityId = comm1.Id,
                Role = "owner",
                Status = "active",
                JoinedAt = now
            });
            db.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = bob.Id,
                CommunityId = comm1.Id,
                Role = "member",
                Status = "banned",
                JoinedAt = now
            });

            var jr = new JoinRequest
            {
                CommunityId = comm1.Id,
                UserId = bob.Id,
                Status = "pending",
                RequestedAt = now,
                UpdatedAt = now
            };
            db.JoinRequests.Add(jr);
            await db.SaveChangesAsync();
            PendingJoinRequestId = jr.Id;
        }

        public async Task<string> LoginAsync(HttpClient client, string username, string password)
        {
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
            {
                Username = username,
                Password = password
            });
            loginResponse.EnsureSuccessStatusCode();
            var login = await loginResponse.Content.ReadFromJsonAsync<LoginResultDTO>();
            Assert.False(string.IsNullOrWhiteSpace(login?.Token));
            return login!.Token;
        }
    }
}
