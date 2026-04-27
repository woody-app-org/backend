using Woody.Application.Billing;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Security;
using Woody.Application.Validation;
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
    private readonly IAuthSessionService _authSessions;

    public RegisterHandler(
        IUserRepository users,
        IUserSubscriptionRepository subscriptions,
        IEmailVerificationCodeRepository emailVerificationCodes,
        IPasswordHasher passwordHasher,
        IDefaultCommunityBootstrap defaultCommunity,
        IAuthSessionService authSessions)
    {
        _users = users;
        _subscriptions = subscriptions;
        _emailVerificationCodes = emailVerificationCodes;
        _passwordHasher = passwordHasher;
        _defaultCommunity = defaultCommunity;
        _authSessions = authSessions;
    }

    public async Task<LoginResultDTO> HandleAsync(RegisterRequestDTO request, CancellationToken cancellationToken = default)
    {
        if (!InputValidator.TryNormalizeRequiredText(
                request.Username,
                "Nome de utilizador",
                InputValidationLimits.UsernameMaxLength,
                out var username,
                out var error))
            throw new ArgumentException(error);

        if (!InputValidator.TryNormalizeRequiredText(
                request.Email,
                "E-mail",
                InputValidationLimits.EmailMaxLength,
                out var email,
                out error))
            throw new ArgumentException(error);
        email = email.ToLowerInvariant();

        if (!InputValidator.TryNormalizeRequiredText(
                request.Password,
                "Senha",
                InputValidationLimits.PasswordMaxLength,
                out var password,
                out error,
                minLength: 8))
            throw new ArgumentException(error);

        if (!InputValidator.TryNormalizeRequiredText(
                request.Cpf,
                "CPF",
                InputValidationLimits.CpfMaxLength,
                out var cpf,
                out error))
            throw new ArgumentException(error);

        if (!InputValidator.TryNormalizeHttpsImageUrl(request.AvatarUrl, out var avatarUrl, out error))
            throw new ArgumentException(error);

        if (await _users.ExistsUsernameAsync(username))
            throw new InvalidOperationException("Não foi possível concluir o cadastro. Verifique os dados e tente novamente.");
        if (await _users.ExistsEmailAsync(email))
            throw new InvalidOperationException("Não foi possível concluir o cadastro. Verifique os dados e tente novamente.");

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
            Password = _passwordHasher.HashPassword(password),
            Role = "User",
            DisplayName = username,
            Cpf = cpf,
            BirthDate = birthDate,
            ProfilePic = avatarUrl,
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

        return await _authSessions.CreateSessionAsync(user, subscription, cancellationToken);
    }
}
