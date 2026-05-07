using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Beta;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.UseCases.Auth.Register;
using Woody.Domain.Entities;

namespace Woody.Application.Tests;

public class RegisterHandlerTests
{
    private static RegisterRequestDTO ValidRequest(string? inviteCode = null) =>
        new()
        {
            Username = "novauser",
            Email = "nova@example.com",
            Password = "senha1234",
            Cpf = "52998224725",
            BirthDate = "1995-03-15",
            InviteCode = inviteCode
        };

    [Fact]
    public async Task BetaEnabled_WithoutInviteCode_ThrowsArgumentException()
    {
        var sut = CreateSut(betaEnabled: true);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.HandleAsync(ValidRequest(inviteCode: null)));

        Assert.Equal(BetaInviteMessages.RequiredWhenBetaActive, ex.Message);
    }

    [Fact]
    public async Task BetaEnabled_WithInvalidInvite_ThrowsArgumentException()
    {
        var beta = new Mock<IBetaInviteRepository>();
        beta.Setup(x => x.TryConsumeOneUseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut(betaEnabled: true, betaInvites: beta);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.HandleAsync(ValidRequest(inviteCode: "INVALID")));

        Assert.Equal(BetaInviteMessages.InvalidForRegistration, ex.Message);
    }

    [Fact]
    public async Task BetaEnabled_WithValidInvite_CompletesRegistration()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.ExistsUsernameAsync(It.IsAny<string>())).ReturnsAsync(false);
        users.Setup(x => x.ExistsEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        users.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var codes = new Mock<IEmailVerificationCodeRepository>();
        codes.Setup(x => x.HasConsumedCodeForEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var beta = new Mock<IBetaInviteRepository>();
        beta.Setup(x => x.TryConsumeOneUseAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions.Setup(x => x.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        subscriptions.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var bootstrap = new Mock<IDefaultCommunityBootstrap>();
        bootstrap.Setup(x => x.EnsureUserInDefaultCommunityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        User? capturedUser = null;
        users.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IAuthSessionService>();
        auth.Setup(x => x.CreateSessionAsync(It.IsAny<User>(), It.IsAny<UserSubscription?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResultDTO());

        var sut = CreateSut(
            betaEnabled: true,
            users: users,
            emailCodes: codes,
            betaInvites: beta,
            subscriptions: subscriptions,
            defaultCommunity: bootstrap,
            authSessions: auth);

        await sut.HandleAsync(ValidRequest(inviteCode: "  abc123 "));

        Assert.NotNull(capturedUser);
        Assert.Equal(42, capturedUser!.InviteId);
        beta.Verify(x => x.TryConsumeOneUseAsync("ABC123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BetaDisabled_IgnoresInviteAndRegistersWithoutInviteId()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.ExistsUsernameAsync(It.IsAny<string>())).ReturnsAsync(false);
        users.Setup(x => x.ExistsEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        users.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var codes = new Mock<IEmailVerificationCodeRepository>();
        codes.Setup(x => x.HasConsumedCodeForEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var beta = new Mock<IBetaInviteRepository>();

        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions.Setup(x => x.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        subscriptions.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var bootstrap = new Mock<IDefaultCommunityBootstrap>();
        bootstrap.Setup(x => x.EnsureUserInDefaultCommunityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        User? capturedUser = null;
        users.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IAuthSessionService>();
        auth.Setup(x => x.CreateSessionAsync(It.IsAny<User>(), It.IsAny<UserSubscription?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResultDTO());

        var sut = CreateSut(
            betaEnabled: false,
            users: users,
            emailCodes: codes,
            betaInvites: beta,
            subscriptions: subscriptions,
            defaultCommunity: bootstrap,
            authSessions: auth);

        await sut.HandleAsync(ValidRequest(inviteCode: "SHOULD-BE-IGNORED"));

        Assert.NotNull(capturedUser);
        Assert.Null(capturedUser!.InviteId);
        beta.Verify(x => x.TryConsumeOneUseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RegisterHandler CreateSut(
        bool betaEnabled,
        Mock<IUserRepository>? users = null,
        Mock<IEmailVerificationCodeRepository>? emailCodes = null,
        Mock<IBetaInviteRepository>? betaInvites = null,
        Mock<IUserSubscriptionRepository>? subscriptions = null,
        Mock<IDefaultCommunityBootstrap>? defaultCommunity = null,
        Mock<IAuthSessionService>? authSessions = null)
    {
        users ??= CreateDefaultUsersMock();
        emailCodes ??= CreateDefaultEmailCodesMock();
        betaInvites ??= CreateDefaultBetaMock();
        subscriptions ??= CreateDefaultSubscriptionsMock();
        defaultCommunity ??= CreateDefaultBootstrapMock();
        authSessions ??= CreateDefaultAuthMock();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hash");

        var uow = new Mock<IWoodyUnitOfWork>();
        uow.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((fn, _) => fn());

        return new RegisterHandler(
            users.Object,
            subscriptions.Object,
            emailCodes.Object,
            hasher.Object,
            defaultCommunity.Object,
            authSessions.Object,
            betaInvites.Object,
            uow.Object,
            Options.Create(new BetaAccessOptions { Enabled = betaEnabled }));
    }

    private static Mock<IUserRepository> CreateDefaultUsersMock()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.ExistsUsernameAsync(It.IsAny<string>())).ReturnsAsync(false);
        users.Setup(x => x.ExistsEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        users.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        users.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        return users;
    }

    private static Mock<IEmailVerificationCodeRepository> CreateDefaultEmailCodesMock()
    {
        var codes = new Mock<IEmailVerificationCodeRepository>();
        codes.Setup(x => x.HasConsumedCodeForEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return codes;
    }

    private static Mock<IBetaInviteRepository> CreateDefaultBetaMock()
    {
        var beta = new Mock<IBetaInviteRepository>();
        beta.Setup(x => x.TryConsumeOneUseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return beta;
    }

    private static Mock<IUserSubscriptionRepository> CreateDefaultSubscriptionsMock()
    {
        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions.Setup(x => x.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        subscriptions.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return subscriptions;
    }

    private static Mock<IDefaultCommunityBootstrap> CreateDefaultBootstrapMock()
    {
        var bootstrap = new Mock<IDefaultCommunityBootstrap>();
        bootstrap.Setup(x => x.EnsureUserInDefaultCommunityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return bootstrap;
    }

    private static Mock<IAuthSessionService> CreateDefaultAuthMock()
    {
        var auth = new Mock<IAuthSessionService>();
        auth.Setup(x => x.CreateSessionAsync(It.IsAny<User>(), It.IsAny<UserSubscription?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResultDTO());
        return auth;
    }
}
