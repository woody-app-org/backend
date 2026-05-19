using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.UseCases.Auth.Login;
using Woody.Domain.Entities;

namespace Woody.Application.Tests;

public class LoginHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsGenericError_WhenUserDoesNotExist()
    {
        var sut = CreateHandler(user: null, passwordOutcome: new PasswordVerificationOutcome(false, false));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.HandleAsync(new LoginRequestDTO { Email = "missing@example.com", Password = "wrong" }));

        Assert.Equal(LoginHandler.InvalidCredentialsMessage, ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericError_WhenPasswordIsWrong()
    {
        var user = new User { Id = 7, Username = "bea", Email = "bea@example.com", Password = "hash", Role = "User" };
        var sut = CreateHandler(user, new PasswordVerificationOutcome(false, false));

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.HandleAsync(new LoginRequestDTO { Username = "bea", Password = "wrong" }));

        Assert.Equal(LoginHandler.InvalidCredentialsMessage, ex.Message);
    }

    [Fact]
    public async Task HandleAsync_StripsWhitespaceFromPasswordBeforeVerification()
    {
        var user = new User { Id = 7, Username = "bea", Email = "bea@example.com", Password = "hash", Role = "User" };
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.VerifyPasswordWithOutcome("hash", "correct"))
            .Returns(new PasswordVerificationOutcome(true, false));
        hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("new-hash");

        var authSessions = new Mock<IAuthSessionService>();
        authSessions
            .Setup(x => x.CreateSessionAsync(user, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResultDTO
            {
                Token = "access-token",
                RefreshToken = "refresh-token",
                User = new AuthUserDto { Id = "7", Username = "bea", Email = "bea@example.com" }
            });

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByUsernameOrEmailAsync("bea")).ReturnsAsync(user);
        users.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions.Setup(x => x.GetByUserIdNoTrackingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSubscription?)null);

        var sut = new LoginHandler(
            users.Object,
            subscriptions.Object,
            hasher.Object,
            authSessions.Object,
            new InMemoryLoginLockoutRepository(),
            Options.Create(new AuthSecurityOptions()));

        await sut.HandleAsync(new LoginRequestDTO { Username = "bea", Password = " cor rect " });

        hasher.Verify(x => x.VerifyPasswordWithOutcome("hash", "correct"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CreatesSession_WhenCredentialsAreValid()
    {
        var user = new User { Id = 7, Username = "bea", Email = "bea@example.com", Password = "hash", Role = "User" };
        var expected = new LoginResultDTO
        {
            Token = "access-token",
            RefreshToken = "refresh-token",
            User = new AuthUserDto { Id = "7", Username = "bea", Email = "bea@example.com" }
        };
        var authSessions = new Mock<IAuthSessionService>();
        authSessions
            .Setup(x => x.CreateSessionAsync(user, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var sut = CreateHandler(user, new PasswordVerificationOutcome(true, false), authSessions: authSessions);

        var result = await sut.HandleAsync(new LoginRequestDTO { Username = "bea", Password = "correct" });

        Assert.Same(expected, result);
        authSessions.Verify(x => x.CreateSessionAsync(user, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LocksOutAfterConfiguredFailures()
    {
        var user = new User { Id = 7, Username = "bea", Email = "bea@example.com", Password = "hash", Role = "User" };
        var lockouts = new InMemoryLoginLockoutRepository();
        var sut = CreateHandler(
            user,
            new PasswordVerificationOutcome(false, false),
            lockouts: lockouts,
            options: new AuthSecurityOptions { MaxFailedLoginAttempts = 2, LockoutMinutes = 15 });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.HandleAsync(new LoginRequestDTO { Username = "bea", Password = "wrong" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.HandleAsync(new LoginRequestDTO { Username = "bea", Password = "wrong" }));

        var lockout = await lockouts.GetByNormalizedLoginAsync("bea");
        Assert.NotNull(lockout);
        Assert.True(lockout.LockoutEndAt > DateTime.UtcNow);
    }

    private static LoginHandler CreateHandler(
        User? user,
        PasswordVerificationOutcome passwordOutcome,
        Mock<IAuthSessionService>? authSessions = null,
        ILoginLockoutRepository? lockouts = null,
        AuthSecurityOptions? options = null)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        users.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var subscriptions = new Mock<IUserSubscriptionRepository>();
        subscriptions.Setup(x => x.GetByUserIdNoTrackingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSubscription?)null);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.VerifyPasswordWithOutcome(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(passwordOutcome);
        hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("new-hash");

        if (authSessions == null)
        {
            authSessions = new Mock<IAuthSessionService>();
            authSessions
                .Setup(x => x.CreateSessionAsync(It.IsAny<User>(), It.IsAny<UserSubscription?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoginResultDTO
                {
                    Token = "access-token",
                    RefreshToken = "refresh-token",
                    User = new AuthUserDto()
                });
        }

        return new LoginHandler(
            users.Object,
            subscriptions.Object,
            hasher.Object,
            authSessions.Object,
            lockouts ?? new InMemoryLoginLockoutRepository(),
            Options.Create(options ?? new AuthSecurityOptions()));
    }

    private sealed class InMemoryLoginLockoutRepository : ILoginLockoutRepository
    {
        private readonly Dictionary<string, LoginLockout> _rows = new();

        public Task<LoginLockout?> GetByNormalizedLoginAsync(
            string normalizedLogin,
            CancellationToken cancellationToken = default)
        {
            _rows.TryGetValue(normalizedLogin, out var row);
            return Task.FromResult(row);
        }

        public void Add(LoginLockout lockout) => _rows[lockout.NormalizedLogin] = lockout;

        public void Remove(LoginLockout lockout) => _rows.Remove(lockout.NormalizedLogin);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
