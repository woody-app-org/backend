using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.UseCases.Auth.Login;
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
            builder.Services.AddScoped<LoginHandler>();

        }
    }
}