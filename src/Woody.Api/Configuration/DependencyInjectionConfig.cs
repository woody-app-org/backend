using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Auth.Register;
using Woody.Infrastructure.Repositories;
using Woody.Infrastructure.Security;
using Woody.Infrastructure.Services;

namespace Woody.Api.Configuration;

public static class DependencyInjectionConfig
{
    public static void ResolveDependencyInjection(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
        builder.Services.AddScoped<IDefaultCommunityBootstrap, DefaultCommunityBootstrap>();
        builder.Services.AddScoped<LoginHandler>();
        builder.Services.AddScoped<RegisterHandler>();
    }
}
