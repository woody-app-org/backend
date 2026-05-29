using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Email;
using Woody.Application.Interfaces.Security;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;
using Woody.Infrastructure.Security;

namespace Woody.Application.Tests;

public class PasswordResetServiceTests
{
    private const string Email = "user@example.com";
    private const string OldPassword = "OldPassword1!";
    private const string NewPassword = "NewPassword1!";

    [Fact]
    public async Task RequestAsync_ExistingAndMissingEmail_ReturnSameGenericResponse()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var sut = CreateSut(ctx, out var emailSender);

        var existing = await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = Email });
        var missing = await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = "ghost@example.com" });

        Assert.Equal(PasswordResetService.GenericRequestMessage, existing.Message);
        Assert.Equal(PasswordResetService.GenericRequestMessage, missing.Message);
        Assert.Equal("us*****@example.com", existing.MaskedEmail);
        Assert.Equal("gh*****@example.com", missing.MaskedEmail);
        emailSender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestAsync_InvalidatesOnlyPasswordResetCodes()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var now = DateTime.UtcNow;
        ctx.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.EmailConfirmation,
            Email = Email,
            CodeHash = "hash",
            ExpiresAt = now.AddMinutes(10),
            MaxAttempts = 5,
            CreatedAt = now,
            UpdatedAt = now
        });
        ctx.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.PasswordReset,
            Email = Email,
            CodeHash = "old-reset",
            ExpiresAt = now.AddMinutes(10),
            MaxAttempts = 5,
            CreatedAt = now.AddMinutes(-1),
            UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx, out _);
        await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = Email });

        var emailConfirmation = await ctx.EmailVerificationCodes
            .SingleAsync(x => x.Purpose == VerificationCodePurpose.EmailConfirmation);
        var oldReset = await ctx.EmailVerificationCodes
            .SingleAsync(x => x.CodeHash == "old-reset");
        var activeResetCount = await ctx.EmailVerificationCodes
            .CountAsync(x => x.Purpose == VerificationCodePurpose.PasswordReset && x.InvalidatedAt == null);

        Assert.Null(emailConfirmation.InvalidatedAt);
        Assert.NotNull(oldReset.InvalidatedAt);
        Assert.Equal(1, activeResetCount);
    }

    [Fact]
    public async Task VerifyCodeAsync_WrongCode_IncrementsAttempts()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var sut = CreateSut(ctx, out var emailSender);
        await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = Email });
        var code = ExtractCodeFromLastEmail(emailSender);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.VerifyCodeAsync(new VerifyPasswordResetCodeRequestDTO
            {
                Email = Email,
                Code = code == "000000" ? "111111" : "000000"
            }));

        Assert.Equal(PasswordResetService.InvalidCodeMessage, ex.Message);
        var entity = await ctx.EmailVerificationCodes
            .Where(x => x.Purpose == VerificationCodePurpose.PasswordReset)
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
        Assert.Equal(1, entity.AttemptCount);
    }

    [Fact]
    public async Task VerifyCodeAsync_RejectsEmailConfirmationCode()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var hasher = new PasswordHasher();
        var plain = "123456";
        var now = DateTime.UtcNow;
        ctx.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.EmailConfirmation,
            Email = Email,
            CodeHash = hasher.HashPassword(plain),
            ExpiresAt = now.AddMinutes(10),
            AttemptCount = 0,
            MaxAttempts = 5,
            CreatedAt = now,
            UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx, out _);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.VerifyCodeAsync(new VerifyPasswordResetCodeRequestDTO { Email = Email, Code = plain }));

        Assert.Equal(PasswordResetService.InvalidCodeMessage, ex.Message);
    }

    [Fact]
    public async Task ConfirmAsync_ValidToken_ChangesPasswordAndRevokesSessions()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var hasher = new PasswordHasher();
        var authSessions = new Mock<IAuthSessionService>();
        var sut = CreateSut(ctx, out var emailSender, authSessions);
        await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = Email });
        var code = ExtractCodeFromLastEmail(emailSender);
        var verified = await sut.VerifyCodeAsync(new VerifyPasswordResetCodeRequestDTO { Email = Email, Code = code });

        var now = DateTime.UtcNow;
        ctx.RefreshTokenSessions.Add(new RefreshTokenSession
        {
            UserId = 1,
            TokenHash = "active-session",
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        });
        await ctx.SaveChangesAsync();

        var result = await sut.ConfirmAsync(new ConfirmPasswordResetRequestDTO
        {
            ResetToken = verified.ResetToken,
            NewPassword = NewPassword,
            ConfirmPassword = NewPassword
        });

        Assert.Equal(PasswordResetService.SuccessMessage, result.Message);
        var user = await ctx.Users.SingleAsync();
        Assert.True(hasher.VerifyPassword(user.Password, NewPassword));
        Assert.False(hasher.VerifyPassword(user.Password, OldPassword));
        authSessions.Verify(
            x => x.RevokeAllForUserAsync(1, "password_reset", It.IsAny<CancellationToken>()),
            Times.Once);

        var session = await ctx.PasswordResetSessions.SingleAsync();
        Assert.NotNull(session.ConsumedAt);
    }

    [Fact]
    public async Task ConfirmAsync_WeakPassword_Fails()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var sut = CreateSut(ctx, out var emailSender);
        await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = Email });
        var code = ExtractCodeFromLastEmail(emailSender);
        var verified = await sut.VerifyCodeAsync(new VerifyPasswordResetCodeRequestDTO { Email = Email, Code = code });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ConfirmAsync(new ConfirmPasswordResetRequestDTO
            {
                ResetToken = verified.ResetToken,
                NewPassword = "short",
                ConfirmPassword = "short"
            }));

        Assert.Contains("mínimo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmAsync_MismatchedPasswords_Fails()
    {
        await using var ctx = await CreateContextWithUserAsync();
        var sut = CreateSut(ctx, out var emailSender);
        await sut.RequestAsync(new RequestPasswordResetRequestDTO { Email = Email });
        var code = ExtractCodeFromLastEmail(emailSender);
        var verified = await sut.VerifyCodeAsync(new VerifyPasswordResetCodeRequestDTO { Email = Email, Code = code });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ConfirmAsync(new ConfirmPasswordResetRequestDTO
            {
                ResetToken = verified.ResetToken,
                NewPassword = NewPassword,
                ConfirmPassword = "DifferentPassword1!"
            }));

        Assert.Equal("As senhas não coincidem.", ex.Message);
    }

    private static PasswordResetService CreateSut(
        WoodyDbContext ctx,
        out Mock<IEmailSender> emailSender,
        Mock<IAuthSessionService>? authSessions = null)
    {
        emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        authSessions ??= new Mock<IAuthSessionService>();
        authSessions
            .Setup(x => x.RevokeAllForUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new PasswordResetService(
            new UserRepository(ctx),
            new EmailVerificationCodeRepository(ctx),
            new PasswordResetSessionRepository(ctx),
            new PasswordHasher(),
            emailSender.Object,
            authSessions.Object,
            Options.Create(new EmailVerificationOptions { ExpirationMinutes = 10, MaxAttempts = 5 }));
    }

    private static async Task<WoodyDbContext> CreateContextWithUserAsync()
    {
        var ctx = CreateDbContext();
        var hasher = new PasswordHasher();
        ctx.Users.Add(new User
        {
            Username = "testuser",
            Email = Email,
            Password = hasher.HashPassword(OldPassword),
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static WoodyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoodyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WoodyDbContext(options);
    }

    private static string ExtractCodeFromLastEmail(Mock<IEmailSender> emailSender)
    {
        var invocation = emailSender.Invocations.Last(x => x.Method.Name == nameof(IEmailSender.SendAsync));
        var message = (EmailMessage)invocation.Arguments[0];
        var match = Regex.Match(message.HtmlBody, @"letter-spacing: 6px[^>]*>(\d{6})<");
        Assert.True(match.Success, "Expected 6-digit code in email body.");
        return match.Groups[1].Value;
    }
}
