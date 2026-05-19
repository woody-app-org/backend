using Microsoft.Extensions.Options;
using Woody.Application.Beta;
using Woody.Application.Billing;
using Woody.Application.Configuration;
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
    private readonly IBetaInviteRepository _betaInvites;
    private readonly IWoodyUnitOfWork _unitOfWork;
    private readonly IOptions<BetaAccessOptions> _betaAccess;
    private readonly IIdentityVerificationRepository _identityVerifications;

    public RegisterHandler(
        IUserRepository users,
        IUserSubscriptionRepository subscriptions,
        IEmailVerificationCodeRepository emailVerificationCodes,
        IPasswordHasher passwordHasher,
        IDefaultCommunityBootstrap defaultCommunity,
        IAuthSessionService authSessions,
        IBetaInviteRepository betaInvites,
        IWoodyUnitOfWork unitOfWork,
        IOptions<BetaAccessOptions> betaAccess,
        IIdentityVerificationRepository identityVerifications)
    {
        _users = users;
        _subscriptions = subscriptions;
        _emailVerificationCodes = emailVerificationCodes;
        _passwordHasher = passwordHasher;
        _defaultCommunity = defaultCommunity;
        _authSessions = authSessions;
        _betaInvites = betaInvites;
        _unitOfWork = unitOfWork;
        _betaAccess = betaAccess;
        _identityVerifications = identityVerifications;
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

        if (!PasswordInputValidator.TryValidateForRegistration(
                request.Password,
                InputValidationLimits.PasswordMaxLength,
                minLength: 8,
                out var password,
                out error))
            throw new ArgumentException(error);

        if (!InputValidator.TryNormalizeRequiredText(
                request.Cpf,
                "CPF",
                InputValidationLimits.CpfMaxLength,
                out var cpfRaw,
                out error))
            throw new ArgumentException(error);

        var cpf = CpfInputNormalizer.NormalizeDigits(cpfRaw);
        if (cpf.Length != 11 || !CpfInputNormalizer.IsValid(cpf))
            throw new ArgumentException("CPF inválido.");

        if (!InputValidator.TryNormalizeHttpsImageUrl(request.AvatarUrl, out var avatarUrl, out error))
            throw new ArgumentException(error);

        if (await _users.ExistsUsernameAsync(username))
            throw new RegistrationConflictException(
                CheckRegistrationAvailabilityHandler.UsernameTakenMessage,
                "username");
        if (await _users.ExistsEmailAsync(email))
            throw new RegistrationConflictException(
                CheckRegistrationAvailabilityHandler.EmailTakenMessage,
                "email");
        if (await _users.ExistsCpfAsync(cpf, cancellationToken))
            throw new RegistrationConflictException(
                CheckRegistrationAvailabilityHandler.CpfTakenMessage,
                "cpf");

        if (!DateOnly.TryParse(request.BirthDate, out var birthDate))
            throw new ArgumentException("Data de nascimento inválida.");

        var isEmailVerified = await _emailVerificationCodes.HasConsumedCodeForEmailAsync(email, cancellationToken);
        if (!isEmailVerified)
            throw new InvalidOperationException("Confirme o e-mail antes de concluir o cadastro.");

        var now = DateTime.UtcNow;
        var betaEnabled = _betaAccess.Value.Enabled;

        if (betaEnabled)
        {
            var normalizedInvite = BetaInviteNormalizer.Normalize(request.InviteCode);
            if (string.IsNullOrEmpty(normalizedInvite))
                throw new ArgumentException(BetaInviteMessages.RequiredWhenBetaActive);

            User? createdUser = null;
            UserSubscription? createdSubscription = null;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var inviteId = await _betaInvites.TryConsumeOneUseAsync(normalizedInvite, cancellationToken);
                if (inviteId == null)
                    throw new ArgumentException(BetaInviteMessages.InvalidForRegistration);

                var user = CreateUserEntity(
                    username,
                    email,
                    password,
                    cpf,
                    birthDate,
                    avatarUrl,
                    now,
                    isEmailVerified,
                    inviteId);

                await _users.AddAsync(user);
                await _users.SaveChangesAsync();

                await _identityVerifications.AddAsync(new IdentityVerification
                {
                    UserId = user.Id,
                    Status = VerificationStatus.PendingDocument,
                    CreatedAt = now,
                    UpdatedAt = now
                }, cancellationToken);
                await _identityVerifications.SaveChangesAsync(cancellationToken);

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

                createdUser = user;
                createdSubscription = subscription;
            }, cancellationToken);

            return await _authSessions.CreateSessionAsync(createdUser!, createdSubscription!, cancellationToken);
        }

        var userNoBeta = CreateUserEntity(
            username,
            email,
            password,
            cpf,
            birthDate,
            avatarUrl,
            now,
            isEmailVerified,
            inviteId: null);

        await _users.AddAsync(userNoBeta);
        await _users.SaveChangesAsync();

        await _identityVerifications.AddAsync(new IdentityVerification
        {
            UserId = userNoBeta.Id,
            Status = VerificationStatus.PendingDocument,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
        await _identityVerifications.SaveChangesAsync(cancellationToken);

        var subscriptionNoBeta = new UserSubscription
        {
            UserId = userNoBeta.Id,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active,
            PlanCode = BillingPlanCodes.Free,
            BillingProvider = BillingProvider.None,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _subscriptions.AddAsync(subscriptionNoBeta, cancellationToken);
        await _subscriptions.SaveChangesAsync(cancellationToken);

        await _defaultCommunity.EnsureUserInDefaultCommunityAsync(userNoBeta.Id, cancellationToken);

        return await _authSessions.CreateSessionAsync(userNoBeta, subscriptionNoBeta, cancellationToken);
    }

    private User CreateUserEntity(
        string username,
        string email,
        string password,
        string cpf,
        DateOnly birthDate,
        string? avatarUrl,
        DateTime now,
        bool isEmailVerified,
        int? inviteId)
    {
        return new User
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
            InviteId = inviteId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
