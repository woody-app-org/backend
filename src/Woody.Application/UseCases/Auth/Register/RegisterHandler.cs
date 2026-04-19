using Woody.Application.Billing;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.Mapping;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.UseCases.Auth.Register;

public class RegisterHandler
{
    private readonly IUserRepository _users;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IEmailVerificationCodeRepository _emailVerificationCodes;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDefaultCommunityBootstrap _defaultCommunity;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterHandler(
        IUserRepository users,
        IUserSubscriptionRepository subscriptions,
        IEmailVerificationCodeRepository emailVerificationCodes,
        IPasswordHasher passwordHasher,
        IDefaultCommunityBootstrap defaultCommunity,
        IJwtTokenService jwtTokenService)
    {
        _users = users;
        _subscriptions = subscriptions;
        _emailVerificationCodes = emailVerificationCodes;
        _passwordHasher = passwordHasher;
        _defaultCommunity = defaultCommunity;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResultDTO> HandleAsync(RegisterRequestDTO request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.ExistsUsernameAsync(username))
            throw new InvalidOperationException("Nome de utilizador já existe.");
        if (await _users.ExistsEmailAsync(email))
            throw new InvalidOperationException("Email já registado.");

        if (!DateOnly.TryParse(request.BirthDate, out var birthDate))
            throw new ArgumentException("Data de nascimento inválida.");

        var isEmailVerified = await _emailVerificationCodes.HasConsumedCodeForEmailAsync(email, cancellationToken);
        if (!isEmailVerified)
            throw new InvalidOperationException("Confirme o e-mail antes de concluir o cadastro.");

        var now = DateTime.UtcNow;

        var user = new User
        {
            Username = username,
            Email = email,
            Password = _passwordHasher.HashPassword(request.Password),
            Role = "User",
            DisplayName = username,
            Cpf = request.Cpf.Trim(),
            BirthDate = birthDate,
            ProfilePic = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            IsEmailVerified = isEmailVerified,
            EmailVerifiedAt = isEmailVerified ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        var subscription = new UserSubscription
        {
            UserId = user.Id,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active,
            PlanCode = BillingPlanCodes.Free,
            BillingProvider = BillingProvider.None,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _subscriptions.AddAsync(subscription, cancellationToken);
        await _subscriptions.SaveChangesAsync(cancellationToken);

        await _defaultCommunity.EnsureUserInDefaultCommunityAsync(user.Id, cancellationToken);

        var token = _jwtTokenService.GenerateToken(user, subscription);

        return new LoginResultDTO
        {
            Token = token,
            User = AuthUserMapper.From(user, subscription, now)
        };
    }
}
