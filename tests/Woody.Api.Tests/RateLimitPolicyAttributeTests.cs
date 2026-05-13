using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Controllers;

namespace Woody.Api.Tests;

public class RateLimitPolicyAttributeTests
{
    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Login), RateLimitPolicyNames.AuthLogin)]
    [InlineData(typeof(AuthController), nameof(AuthController.Register), RateLimitPolicyNames.AuthRegister)]
    [InlineData(typeof(AuthController), nameof(AuthController.SendVerificationCode), RateLimitPolicyNames.AuthEmailSend)]
    [InlineData(typeof(AuthController), nameof(AuthController.ResendVerificationCode), RateLimitPolicyNames.AuthEmailSend)]
    [InlineData(typeof(AuthController), nameof(AuthController.VerifyEmailCode), RateLimitPolicyNames.AuthEmailVerify)]
    [InlineData(typeof(AuthController), nameof(AuthController.Refresh), RateLimitPolicyNames.AuthRefresh)]
    [InlineData(typeof(MediaController), nameof(MediaController.UploadImage), RateLimitPolicyNames.Upload)]
    [InlineData(typeof(MediaController), nameof(MediaController.GetImage), RateLimitPolicyNames.PublicRead)]
    [InlineData(typeof(PostsController), nameof(PostsController.Create), RateLimitPolicyNames.ContentCreate)]
    [InlineData(typeof(PostsController), nameof(PostsController.CreateComment), RateLimitPolicyNames.ContentComment)]
    [InlineData(typeof(ReportsController), nameof(ReportsController.Create), RateLimitPolicyNames.ReportCreate)]
    [InlineData(typeof(FeedController), nameof(FeedController.GetFeed), RateLimitPolicyNames.AuthenticatedApi)]
    [InlineData(typeof(SearchController), nameof(SearchController.Search), RateLimitPolicyNames.PublicApi)]
    [InlineData(typeof(StripeBillingWebhooksController), nameof(StripeBillingWebhooksController.Post), RateLimitPolicyNames.StripeWebhook)]
    [InlineData(typeof(BetaController), nameof(BetaController.ValidateInvite), RateLimitPolicyNames.BetaInviteValidate)]
    public void SensitiveActions_HaveExpectedRateLimitPolicy(Type controllerType, string actionName, string expectedPolicy)
    {
        var method = controllerType.GetMethod(actionName)
            ?? throw new InvalidOperationException($"Action {controllerType.Name}.{actionName} not found.");

        Assert.Equal(expectedPolicy, GetPolicyName(method));
    }

    [Theory]
    [InlineData(typeof(BillingController), RateLimitPolicyNames.AuthenticatedApi)]
    [InlineData(typeof(ConversationsController), RateLimitPolicyNames.AuthenticatedApi)]
    public void AuthenticatedControllers_HaveUserScopedRateLimitPolicy(Type controllerType, string expectedPolicy)
    {
        Assert.Equal(expectedPolicy, GetPolicyName(controllerType));
    }

    private static string? GetPolicyName(MemberInfo member) =>
        member.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;
}
