using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;

namespace Woody.Infrastructure.Persistence.Seed
{
    public static class DbSeeder
    {
        public static void Seed(WoodyDbContext context)
        {
            SeedUsers(context);
            SeedPosts(context);
            SeedComments(context);
            SeedFollows(context);
            SeedLikes(context);
            SeedTopics(context);
            SeedTopicRelations(context);
        }

        private static void SeedUsers(WoodyDbContext context)
        {
            if (context.Users.Any())
                return;

            PasswordHasher hasher = new();

            var users = new List<User>
            {
                new() { Username = "nicholau", Email = "nicholau@dev.com", Password = hasher.HashPassword("dev123"), Role = "Admin", Bio = "Backend developer focused on .NET and architecture." },
                new() { Username = "mariana", Email = "mariana@design.com", Password = hasher.HashPassword("design123"), Role = "User", Bio = "UI/UX designer apaixonada por produtos digitais." },
                new() { Username = "carlos", Email = "carlos@data.com", Password = hasher.HashPassword("data123"), Role = "User", Bio = "Data enthusiast e curioso por sistemas distribuídos." },
                new() { Username = "ana", Email = "ana@frontend.com", Password = hasher.HashPassword("front123"), Role = "User", Bio = "Frontend engineer e fã de React." },
                new() { Username = "lucas", Email = "lucas@mobile.com", Password = hasher.HashPassword("mobile123"), Role = "User", Bio = "Desenvolvedor mobile e criador de apps indie." }
            };

            users.ForEach(u =>
            {
                u.CreatedAt = DateTime.UtcNow;
                u.UpdatedAt = DateTime.UtcNow;
            });

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        private static void SeedTopics(WoodyDbContext context)
        {
            if (context.Topics.Any())
                return;

            var topics = new List<Topic>
            {
                new() { Name = "dotnet" },
                new() { Name = "architecture" },
                new() { Name = "frontend" },
                new() { Name = "mobile" },
                new() { Name = "design" },
                new() { Name = "data" }
            };

            context.Topics.AddRange(topics);
            context.SaveChanges();
        }

        private static void SeedPosts(WoodyDbContext context)
        {
            if (context.Posts.Any())
                return;

            var users = context.Users.ToList();

            var posts = new List<Post>
            {
                new() {
                    UserId = users[0].Id,
                    Content = "Estou estudando Clean Architecture em .NET e a diferença na organização do código é absurda.",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                },
                new() {
                    UserId = users[1].Id,
                    Content = "Design systems bem feitos economizam meses de retrabalho.",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-25)
                },
                new() {
                    UserId = users[2].Id,
                    Content = "Alguém aqui já trabalhou com processamento de dados em larga escala?",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20)
                },
                new() {
                    UserId = users[3].Id,
                    Content = "React 19 trouxe algumas mudanças interessantes no concurrent rendering.",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-15)
                },
                new() {
                    UserId = users[4].Id,
                    Content = "Publicar um app na Play Store é mais complexo do que parece.",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                }
            };

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
                AuthorId = users[2].Id,
                Content = "Você está aplicando CQRS também?",
                CreatedAt = DateTime.UtcNow
            };

            context.Comments.Add(comment1);
            context.SaveChanges();

            var reply = new Comment
            {
                PostId = post.Id,
                AuthorId = users[0].Id,
                ParentCommentId = comment1.Id,
                Content = "Ainda não, mas está no roadmap.",
                CreatedAt = DateTime.UtcNow
            };

            context.Comments.Add(reply);
            context.SaveChanges();
        }

        private static void SeedTopicRelations(WoodyDbContext context)
        {
            if (context.PostTopics.Any() || context.UserTopics.Any())
                return;

            var users = context.Users.ToList();
            var posts = context.Posts.ToList();
            var topics = context.Topics.ToList();

            var postTopics = new List<PostTopic>
            {
                new() { PostId = posts[0].Id, TopicId = topics.First(t => t.Name == "dotnet").Id },
                new() { PostId = posts[0].Id, TopicId = topics.First(t => t.Name == "architecture").Id },

                new() { PostId = posts[1].Id, TopicId = topics.First(t => t.Name == "design").Id },

                new() { PostId = posts[2].Id, TopicId = topics.First(t => t.Name == "data").Id },

                new() { PostId = posts[3].Id, TopicId = topics.First(t => t.Name == "frontend").Id },

                new() { PostId = posts[4].Id, TopicId = topics.First(t => t.Name == "mobile").Id }
            };

            var userTopics = new List<UserTopic>
            {
                new() { UserId = users[0].Id, TopicId = topics.First(t => t.Name == "dotnet").Id },
                new() { UserId = users[0].Id, TopicId = topics.First(t => t.Name == "architecture").Id },

                new() { UserId = users[1].Id, TopicId = topics.First(t => t.Name == "design").Id },

                new() { UserId = users[2].Id, TopicId = topics.First(t => t.Name == "data").Id },

                new() { UserId = users[3].Id, TopicId = topics.First(t => t.Name == "frontend").Id },

                new() { UserId = users[4].Id, TopicId = topics.First(t => t.Name == "mobile").Id }
            };

            context.PostTopics.AddRange(postTopics);
            context.UserTopics.AddRange(userTopics);
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
}