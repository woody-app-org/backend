using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Billing;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Application.UseCases.Billing;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Tests;

public class BillingCheckoutHardeningTests
{
    [Fact]
    public async Task CreateCheckoutSession_ReusesPendingAttemptAndDoesNotActivatePlan()
    {
        var subscription = new UserSubscription
        {
            UserId = 10,
            Plan = SubscriptionPlan.Free,
            PlanCode = BillingPlanCodes.Free,
            Status = SubscriptionStatus.Active,
            BillingProvider = BillingProvider.None
        };
        var attempts = new Mock<IBillingCheckoutAttemptRepository>();
        attempts
            .SetupSequence(x => x.GetReusableAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingCheckoutAttempt?)null)
            .ReturnsAsync(new BillingCheckoutAttempt { StripeSessionUrl = "https://checkout.stripe.test/reused" });
        attempts
            .Setup(x => x.ClaimOrGetAsync(
                It.IsAny<string>(),
                10,
                BillingCheckoutAttemptSubjectKind.UserSubscription,
                BillingPlanCodes.ProMonthly,
                null,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCheckoutAttempt());
        attempts.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var gateway = new Mock<IBillingCheckoutGateway>();
        gateway
            .Setup(x => x.CreateSubscriptionCheckoutAsync(It.IsAny<BillingCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCheckoutSessionResult(
                true,
                "https://checkout.stripe.test/new",
                null,
                "cus_123",
                "cs_123"));

        var handler = new CreateCheckoutSessionHandler(
            CreateUsers().Object,
            CreateUserSubscriptions(subscription).Object,
            attempts.Object,
            gateway.Object,
            Options.Create(CreateBillingOptions()));

        var first = await handler.HandleAsync(10, new CreateBillingCheckoutRequestDto { PlanCode = BillingPlanCodes.ProMonthly });
        var second = await handler.HandleAsync(10, new CreateBillingCheckoutRequestDto { PlanCode = BillingPlanCodes.ProMonthly });

        Assert.Equal("https://checkout.stripe.test/new", first.Url);
        Assert.Equal("https://checkout.stripe.test/reused", second.Url);
        Assert.Equal(SubscriptionPlan.Free, subscription.Plan);
        gateway.Verify(x => x.CreateSubscriptionCheckoutAsync(It.IsAny<BillingCheckoutSessionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCommunityPremiumCheckout_ReusesPendingAttemptAndDoesNotActivateCommunity()
    {
        var userSubscription = new UserSubscription
        {
            UserId = 10,
            Plan = SubscriptionPlan.Free,
            PlanCode = BillingPlanCodes.Free,
            Status = SubscriptionStatus.Active,
            BillingProvider = BillingProvider.None
        };
        var communitySubscription = new CommunitySubscription
        {
            CommunityId = 55,
            Plan = CommunityPlan.Free,
            PlanCode = CommunityBillingPlanCodes.Free,
            Status = SubscriptionStatus.Active,
            BillingProvider = BillingProvider.None
        };
        var attempts = new Mock<IBillingCheckoutAttemptRepository>();
        attempts
            .SetupSequence(x => x.GetReusableAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingCheckoutAttempt?)null)
            .ReturnsAsync(new BillingCheckoutAttempt { StripeSessionUrl = "https://checkout.stripe.test/community-reused" });
        attempts
            .Setup(x => x.ClaimOrGetAsync(
                It.IsAny<string>(),
                10,
                BillingCheckoutAttemptSubjectKind.CommunityPremium,
                CommunityBillingPlanCodes.PremiumMonthly,
                55,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCheckoutAttempt());
        attempts.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var gateway = new Mock<IBillingCheckoutGateway>();
        gateway
            .Setup(x => x.CreateSubscriptionCheckoutAsync(It.IsAny<BillingCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCheckoutSessionResult(
                true,
                "https://checkout.stripe.test/community-new",
                null,
                "cus_123",
                "cs_community"));

        var handler = new CreateCommunityPremiumCheckoutSessionHandler(
            CreateUsers().Object,
            CreateUserSubscriptions(userSubscription).Object,
            CreateCommunitySubscriptions(communitySubscription).Object,
            CreateCommunities().Object,
            CreateCommunityPermissions().Object,
            attempts.Object,
            gateway.Object,
            Options.Create(CreateBillingOptions()));

        var first = await handler.HandleAsync(10, new CreateCommunityPremiumCheckoutRequestDto { CommunityId = 55 });
        var second = await handler.HandleAsync(10, new CreateCommunityPremiumCheckoutRequestDto { CommunityId = 55 });

        Assert.Equal("https://checkout.stripe.test/community-new", first.Url);
        Assert.Equal("https://checkout.stripe.test/community-reused", second.Url);
        Assert.Equal(CommunityPlan.Free, communitySubscription.Plan);
        gateway.Verify(x => x.CreateSubscriptionCheckoutAsync(It.IsAny<BillingCheckoutSessionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IUserRepository> CreateUsers()
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdTrackedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Email = "user@example.com", Username = "user", Role = "User" });
        return users;
    }

    private static Mock<IUserSubscriptionRepository> CreateUserSubscriptions(UserSubscription subscription)
    {
        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions
            .Setup(x => x.GetByUserIdTrackedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        subscriptions.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return subscriptions;
    }

    private static Mock<ICommunitySubscriptionRepository> CreateCommunitySubscriptions(CommunitySubscription subscription)
    {
        var subscriptions = new Mock<ICommunitySubscriptionRepository>();
        subscriptions
            .Setup(x => x.GetByCommunityIdTrackedAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        subscriptions.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return subscriptions;
    }

    private static Mock<ICommunityRepository> CreateCommunities()
    {
        var communities = new Mock<ICommunityRepository>();
        communities
            .Setup(x => x.ExistsNoTrackingAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return communities;
    }

    private static Mock<ICommunityPermissionService> CreateCommunityPermissions()
    {
        var permissions = new Mock<ICommunityPermissionService>();
        permissions
            .Setup(x => x.CanModerateCommunityAsync(55, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return permissions;
    }

    private static BillingOptions CreateBillingOptions() => new()
    {
        Stripe = new StripeBillingOptions
        {
            SecretKey = "sk_test_configured",
            CheckoutSuccessUrl = "https://woody.test/success",
            CheckoutCancelUrl = "https://woody.test/cancel",
            PriceIds = new StripePriceIdsOptions
            {
                ProMonthly = "price_pro_monthly",
                CommunityPremiumMonthly = "price_community_premium"
            }
        }
    };
}
