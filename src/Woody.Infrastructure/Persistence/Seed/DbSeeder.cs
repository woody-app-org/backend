using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;

namespace Woody.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static void Seed(WoodyDbContext context)
    {
        SeedUsers(context);
        SeedCommunitiesAndMemberships(context);
        SeedPosts(context);
        SeedComments(context);
        SeedFollows(context);
        SeedLikes(context);
    }

    private static void SeedUsers(WoodyDbContext context)
    {
        if (context.Users.Any())
            return;

        var hasher = new PasswordHasher();

        var users = new List<User>
        {
            new()
            {
                Username = "admin",
                Email = "admin@example.com",
                Password = hasher.HashPassword("admin123"),
                Role = "Admin",
                DisplayName = "Admin"
            },
            new()
            {
                Username = "user1",
                Email = "user1@example.com",
                Password = hasher.HashPassword("user123"),
                Role = "User",
                DisplayName = "User Um"
            },
            new()
            {
                Username = "user2",
                Email = "user2@example.com",
                Password = hasher.HashPassword("user234"),
                Role = "User",
                DisplayName = "User Dois"
            },
            new()
            {
                Username = "user3",
                Email = "user3@example.com",
                Password = hasher.HashPassword("user345"),
                Role = "User",
                DisplayName = "User Três"
            },
            new()
            {
                Username = "user4",
                Email = "user4@example.com",
                Password = hasher.HashPassword("user456"),
                Role = "User",
                DisplayName = "User Quatro"
            }
        };

        foreach (var u in users)
        {
            u.CreatedAt = DateTime.UtcNow;
            u.UpdatedAt = DateTime.UtcNow;
        }

        context.Users.AddRange(users);
        context.SaveChanges();
    }

    private static void SeedCommunitiesAndMemberships(WoodyDbContext context)
    {
        var users = context.Users.OrderBy(u => u.Id).ToList();
        if (users.Count == 0)
            return;

        var community = context.Communities.FirstOrDefault(c => c.Slug == "geral");
        if (community == null)
        {
            var owner = users[0];
            community = new Community
            {
                Slug = "geral",
                Name = "Geral",
                Description = "Espaço geral da plataforma",
                Category = "outro",
                Rules = "Seja respeitosa.",
                Visibility = "public",
                OwnerUserId = owner.Id,
                MemberCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Communities.Add(community);
            context.SaveChanges();
        }

        var existingMemberUserIds = context.CommunityMemberships
            .Where(m => m.CommunityId == community.Id)
            .Select(m => m.UserId)
            .ToHashSet();

        foreach (var u in users.Where(u => !existingMemberUserIds.Contains(u.Id)))
        {
            var role = u.Id == community.OwnerUserId ? "owner" : "member";
            context.CommunityMemberships.Add(new CommunityMembership
            {
                UserId = u.Id,
                CommunityId = community.Id,
                Role = role,
                Status = "active",
                JoinedAt = DateTime.UtcNow
            });
        }

        community.MemberCount = context.CommunityMemberships.Count(m => m.CommunityId == community.Id && m.Status == "active");
        community.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();
    }

    private static void SeedPosts(WoodyDbContext context)
    {
        if (context.Posts.Any())
            return;

        var users = context.Users.ToList();
        var community = context.Communities.OrderBy(c => c.Id).FirstOrDefault();
        if (community == null)
            return;

        var posts = users.Select((u, i) => new Post
        {
            UserId = u.Id,
            CommunityId = community.Id,
            Title = $"Post de {u.Username}",
            Content = $"Post inicial do {u.Username}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-i * 5)
        }).ToList();

        context.Posts.AddRange(posts);
        context.SaveChanges();
    }

    private static void SeedComments(WoodyDbContext context)
    {
        if (context.Comments.Any())
            return;

        var post = context.Posts.FirstOrDefault();
        if (post == null)
            return;

        var users = context.Users.ToList();
        if (users.Count < 3)
            return;

        var comment1 = new Comment
        {
            PostId = post.Id,
            AuthorId = users[1].Id,
            Content = "Primeiro comentário",
            CreatedAt = DateTime.UtcNow
        };

        context.Comments.Add(comment1);
        context.SaveChanges();

        var reply = new Comment
        {
            PostId = post.Id,
            AuthorId = users[2].Id,
            ParentCommentId = comment1.Id,
            Content = "Resposta ao comentário",
            CreatedAt = DateTime.UtcNow
        };

        context.Comments.Add(reply);
        context.SaveChanges();
    }

    private static void SeedFollows(WoodyDbContext context)
    {
        if (context.Follows.Any())
            return;

        var users = context.Users.ToList();
        if (users.Count < 4)
            return;

        var follows = new List<Follow>
        {
            new() { FollowingUserId = users[1].Id, FollowedUserId = users[0].Id },
            new() { FollowingUserId = users[2].Id, FollowedUserId = users[0].Id },
            new() { FollowingUserId = users[3].Id, FollowedUserId = users[1].Id }
        };

        foreach (var f in follows)
            f.CreatedAt = DateTime.UtcNow;

        context.Follows.AddRange(follows);
        context.SaveChanges();
    }

    private static void SeedLikes(WoodyDbContext context)
    {
        if (context.Likes.Any())
            return;

        var users = context.Users.ToList();
        var post = context.Posts.FirstOrDefault();
        var comment = context.Comments.FirstOrDefault();
        if (post == null || users.Count < 3 || comment == null)
            return;

        var likes = new List<Like>
        {
            new()
            {
                UserId = users[1].Id,
                TargetType = LikeTargetType.Post,
                TargetId = post.Id,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = users[2].Id,
                TargetType = LikeTargetType.Comment,
                TargetId = comment.Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Likes.AddRange(likes);
        context.SaveChanges();
    }
}
