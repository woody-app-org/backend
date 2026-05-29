using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces.Email;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Repositories;
using Woody.Infrastructure.Security;

namespace Woody.Application.Tests;

public class EmailVerificationServiceRegressionTests
{
    [Fact]
    public async Task ConfirmCodeAsync_UsesOnlyEmailConfirmationPurpose()
    {
        await using var ctx = CreateDbContext();
        var hasher = new PasswordHasher();
        const string email = "new@example.com";
        var now = DateTime.UtcNow;

        ctx.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.PasswordReset,
            Email = email,
            CodeHash = hasher.HashPassword("654321"),
            ExpiresAt = now.AddMinutes(10),
            AttemptCount = 0,
            MaxAttempts = 5,
            CreatedAt = now,
            UpdatedAt = now
        });
        ctx.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.EmailConfirmation,
            Email = email,
            CodeHash = hasher.HashPassword("123456"),
            ExpiresAt = now.AddMinutes(10),
            AttemptCount = 0,
            MaxAttempts = 5,
            CreatedAt = now.AddMinutes(1),
            UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        var emailSender = new Mock<IEmailSender>();
        var sut = new EmailVerificationService(
            new UserRepository(ctx),
            new EmailVerificationCodeRepository(ctx),
            hasher,
            emailSender.Object,
            Options.Create(new EmailVerificationOptions { ExpirationMinutes = 10, MaxAttempts = 5 }));

        var result = await sut.ConfirmCodeAsync(new ConfirmEmailVerificationCodeRequestDTO
        {
            Email = email,
            Code = "123456"
        });

        Assert.True(result.Verified);
        var resetCode = await ctx.EmailVerificationCodes
            .SingleAsync(x => x.Purpose == VerificationCodePurpose.PasswordReset);
        Assert.Null(resetCode.ConsumedAt);
    }

    private static WoodyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoodyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WoodyDbContext(options);
    }
}
