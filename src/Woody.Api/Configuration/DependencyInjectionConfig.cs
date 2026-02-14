using Woody.Application.Interfaces.Security;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Posts.CreatePost;
using Woody.Application.UseCases.Posts.GetPost;
using Woody.Domain.Interfaces;
using Woody.Infrastructure.Repositories;
using Woody.Infrastructure.Security;

namespace Woody.Api.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static void ResolveDependencyInjection(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();
            builder.Services.AddScoped<LoginHandler>();
            builder.Services.AddScoped<CreatePostHandler>();
            builder.Services.AddScoped<GetPostHandler>();

        }
    }
}