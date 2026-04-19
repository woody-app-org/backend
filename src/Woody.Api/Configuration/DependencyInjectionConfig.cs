using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Application.Interfaces.Email;
using Woody.Application.Interfaces.Security;
using Woody.Application.Services;
using Woody.Application.UseCases.Auth.Login;
using Woody.Application.UseCases.Auth.Register;
using Woody.Application.UseCases.Billing;
using Woody.Infrastructure.Billing.StripePayments;
using Woody.Infrastructure.Repositories;
using Woody.Infrastructure.Security;
using Woody.Infrastructure.Services.Email;

namespace Woody.Api.Configuration;

public static class DependencyInjectionConfig
{
    public static void ResolveDependencyInjection(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IBillingWebhookReceiptRepository, BillingWebhookReceiptRepository>();
        builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
        builder.Services.AddScoped<IBillingSubscriptionGateway, StripeBillingSubscriptionGateway>();
        builder.Services.AddScoped<IBillingCheckoutGateway, StripeBillingCheckoutGateway>();
        builder.Services.AddScoped<IBillingCustomerPortalGateway, StripeBillingCustomerPortalGateway>();
        builder.Services.AddSingleton<IBillingWebhookSignatureVerifier, StripeBillingWebhookSignatureVerifier>();
        builder.Services.AddScoped<IStripeWebhookBillingProcessor, StripeBillingWebhookProcessor>();
        builder.Services.AddScoped<CreateCheckoutSessionHandler>();
        builder.Services.AddScoped<CreateCustomerPortalSessionHandler>();
        builder.Services.AddScoped<IUserEntitlementService, UserEntitlementService>();
        builder.Services.AddScoped<IEmailVerificationCodeRepository, EmailVerificationCodeRepository>();
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
        builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });
        builder.Services.AddScoped<IDefaultCommunityBootstrap, DefaultCommunityBootstrap>();
        builder.Services.AddScoped<LoginHandler>();
        builder.Services.AddScoped<RegisterHandler>();
    }
}
