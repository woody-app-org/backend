using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.Services;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Auth.Register;
using Woody.Infrastructure.Repositories;
using Woody.Infrastructure.Security;

namespace Woody.Api.Configuration;

public static class DependencyInjectionConfig
{
    public static void ResolveDependencyInjection(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IPostRepository, PostRepository>();
        builder.Services.AddScoped<ILikeRepository, LikeRepository>();
        builder.Services.AddScoped<ICommentRepository, CommentRepository>();
        builder.Services.AddScoped<IFollowRepository, FollowRepository>();
        builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();
        builder.Services.AddScoped<ICommunityMembershipRepository, CommunityMembershipRepository>();
        builder.Services.AddScoped<IJoinRequestRepository, JoinRequestRepository>();
        builder.Services.AddScoped<IContentReportRepository, ContentReportRepository>();
        builder.Services.AddScoped<IPostEnrichmentService, PostEnrichmentService>();
        builder.Services.AddScoped<IFeedService, FeedService>();
        builder.Services.AddScoped<ICommunityPermissionService, CommunityPermissionService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
        builder.Services.AddScoped<IDefaultCommunityBootstrap, DefaultCommunityBootstrap>();
        builder.Services.AddScoped<LoginHandler>();
        builder.Services.AddScoped<RegisterHandler>();
    }
}
