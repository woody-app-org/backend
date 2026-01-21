using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Persistence.Seed
{
    public class DbSeeder
    {
        public static void Seed(WoodyDbContext context)
        {
            string[] usernames = { "admin", "user1", "user2", "user3", "user4", "user5", "user6", "user7", "user8", "user9", "user10" };
            string[] emails = { "admin@example.com", "user1@example.com", "user2@example.com", "user3@example.com", "user4@example.com", "user5@example.com", "user6@example.com", "user7@example.com", "user8@example.com", "user9@example.com", "user10@example.com" };
            for (int i = 0; i < usernames.Length; i++)
            {
                if (!context.Users.Any(u => u.Username == usernames[i]))
                {
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = usernames[i],
                        Email = emails[i],
                        PasswordHash = "hashed_password",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Users.Add(user);
                }
            }
            context.SaveChanges();
        }
    }
}