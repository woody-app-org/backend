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
        SeedPosts(context);
        SeedComments(context);
        SeedFollows(context);
        SeedLikes(context);
    }

    private static void SeedUsers(WoodyDbContext context)
    {
        if (context.Users.Any())
            return;

        PasswordHasher hasher = new();

        var users = new List<User>
        {
            new() { Username = "admin", Email = "admin@example.com", Password = hasher.HashPassword("admin123"), Role = "Admin" },
            new() { Username = "user1", Email = "user1@example.com", Password = hasher.HashPassword("user123"), Role = "User" },
            new() { Username = "user2", Email = "user2@example.com", Password = hasher.HashPassword("user234"), Role = "User" },
            new() { Username = "user3", Email = "user3@example.com", Password = hasher.HashPassword("user345"), Role = "User" },
            new() { Username = "user4", Email = "user4@example.com", Password = hasher.HashPassword("user456"), Role = "User" }
        };

        users.ForEach(u =>
        {
            u.CreatedAt = DateTime.UtcNow;
            u.UpdatedAt = DateTime.UtcNow;
        });

        context.Users.AddRange(users);
        context.SaveChanges();
    }

    private static void SeedPosts(WoodyDbContext context)
    {
        if (context.Posts.Any())
            return;

        var users = context.Users.ToList();

        var posts = users.Select((u, i) => new Post
        {
            UserId = u.Id,
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

        var post = context.Posts.First();
        var users = context.Users.ToList();

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

        var follows = new List<Follow>
        {
            new() { FollowingUserId = users[1].Id, FollowedUserId = users[0].Id },
            new() { FollowingUserId = users[2].Id, FollowedUserId = users[0].Id },
            new() { FollowingUserId = users[3].Id, FollowedUserId = users[1].Id }
        };

        follows.ForEach(f => f.CreatedAt = DateTime.UtcNow);

        context.Follows.AddRange(follows);
        context.SaveChanges();
    }

    private static void SeedLikes(WoodyDbContext context)
    {
        if (context.Likes.Any())
            return;

        var users = context.Users.ToList();
        var post = context.Posts.First();
        var comment = context.Comments.First();

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