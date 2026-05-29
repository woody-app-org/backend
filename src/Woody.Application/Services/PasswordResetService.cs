using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Email;
using Woody.Application.Interfaces.Security;
using Woody.Application.Utilities;
using Woody.Application.Validation;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    public const string GenericRequestMessage =
        "Se este e-mail estiver cadastrado, enviaremos um código.";

    public const string InvalidCodeMessage = "Código inválido.";
    public const string SuccessMessage = "Senha alterada com sucesso.";

    private static readonly Regex NonDigitRegex = new(@"\D", RegexOptions.Compiled);

    private readonly IUserRepository _users;
    private readonly IEmailVerificationCodeRepository _codes;
    private readonly IPasswordResetSessionRepository _resetSessions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IAuthSessionService _authSessions;
    private readonly EmailVerificationOptions _options;

    public PasswordResetService(
        IUserRepository users,
        IEmailVerificationCodeRepository codes,
        IPasswordResetSessionRepository resetSessions,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IAuthSessionService authSessions,
        IOptions<EmailVerificationOptions> options)
    {
        _users = users;
        _codes = codes;
        _resetSessions = resetSessions;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _authSessions = authSessions;
        _options = options.Value;
    }

    public async Task<RequestPasswordResetResponseDTO> RequestAsync(
        RequestPasswordResetRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var response = BuildGenericRequestResponse(email);

        var user = await _users.GetByEmailAsync(email);
        if (user is null)
            return response;

        var now = DateTime.UtcNow;
        await _codes.InvalidateActiveByEmailAndPurposeAsync(
            email,
            VerificationCodePurpose.PasswordReset,
            now,
            cancellationToken);
        await _resetSessions.InvalidateActiveForUserAsync(user.Id, now, cancellationToken);

        var plainCode = GenerateSixDigitCode();
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var entity = new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.PasswordReset,
            Email = email,
            UserId = user.Id,
            CodeHash = _passwordHasher.HashPassword(plainCode),
            ExpiresAt = expiresAt,
            AttemptCount = 0,
            MaxAttempts = _options.MaxAttempts,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _codes.AddAsync(entity, cancellationToken);
        await _codes.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailSender.SendAsync(
                new EmailMessage
                {
                    To = email,
                    Subject = "Código para redefinir sua senha na Woody",
                    HtmlBody = BuildHtmlBody(plainCode, _options.ExpirationMinutes),
                    TextBody = BuildTextBody(plainCode, _options.ExpirationMinutes)
                },
                cancellationToken);
        }
        catch
        {
            entity.InvalidatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _codes.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Não foi possível enviar o código de recuperação. Tente novamente.");
        }

        return response;
    }

    public async Task<VerifyPasswordResetCodeResponseDTO> VerifyCodeAsync(
        VerifyPasswordResetCodeRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var code = NormalizeCode(request.Code);
        var now = DateTime.UtcNow;

        var latestCode = await _codes.GetLatestByEmailAndPurposeAsync(
            email,
            VerificationCodePurpose.PasswordReset,
            cancellationToken);
        if (latestCode is null)
            throw new ArgumentException(InvalidCodeMessage);

        if (latestCode.ConsumedAt.HasValue)
            throw new InvalidOperationException("Código já utilizado.");

        if (latestCode.InvalidatedAt.HasValue)
            throw new ArgumentException(InvalidCodeMessage);

        if (latestCode.ExpiresAt <= now)
            throw new InvalidOperationException("Código expirado. Solicite um novo código.");

        if (latestCode.AttemptCount >= latestCode.MaxAttempts)
            throw new InvalidOperationException("Número máximo de tentativas excedido. Solicite um novo código.");

        var isValidCode = _passwordHasher.VerifyPassword(latestCode.CodeHash, code);
        if (!isValidCode)
        {
            latestCode.AttemptCount += 1;
            latestCode.UpdatedAt = now;
            if (latestCode.AttemptCount >= latestCode.MaxAttempts)
                latestCode.InvalidatedAt = now;

            await _codes.SaveChangesAsync(cancellationToken);
            throw new ArgumentException(InvalidCodeMessage);
        }

        var user = await _users.GetByEmailAsync(email);
        if (user is null)
            throw new ArgumentException(InvalidCodeMessage);

        latestCode.ConsumedAt = now;
        latestCode.UpdatedAt = now;
        latestCode.UserId = user.Id;

        await _resetSessions.InvalidateActiveForUserAsync(user.Id, now, cancellationToken);

        var resetToken = GenerateResetToken();
        var tokenHash = HashResetToken(resetToken);
        var sessionExpiresAt = now.AddMinutes(_options.ExpirationMinutes);

        await _resetSessions.AddAsync(
            new PasswordResetSession
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = sessionExpiresAt,
                CreatedAt = now
            },
            cancellationToken);

        await _codes.SaveChangesAsync(cancellationToken);
        await _resetSessions.SaveChangesAsync(cancellationToken);

        var expiresInSeconds = (int)Math.Max(1, (sessionExpiresAt - now).TotalSeconds);
        return new VerifyPasswordResetCodeResponseDTO
        {
            ResetToken = resetToken,
            ExpiresInSeconds = expiresInSeconds
        };
    }

    public async Task<ConfirmPasswordResetResponseDTO> ConfirmAsync(
        ConfirmPasswordResetRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ResetToken))
            throw new ArgumentException("Token de recuperação inválido.");

        if (request.ResetToken.Length > InputValidationLimits.RefreshTokenMaxLength)
            throw new ArgumentException("Token de recuperação inválido.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ArgumentException("Senha é obrigatória.");

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            throw new ArgumentException("As senhas não coincidem.");

        if (!PasswordInputValidator.TryValidateForRegistration(
                request.NewPassword,
                InputValidationLimits.PasswordMaxLength,
                minLength: 8,
                out var password,
                out var passwordError))
            throw new ArgumentException(passwordError);

        var now = DateTime.UtcNow;
        var tokenHash = HashResetToken(request.ResetToken.Trim());
        var session = await _resetSessions.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (session is null || !session.IsActiveAt(now))
            throw new ArgumentException("Token de recuperação inválido ou expirado.");

        var user = await _users.GetByIdTrackedAsync(session.UserId, cancellationToken);
        if (user is null)
            throw new ArgumentException("Token de recuperação inválido ou expirado.");

        user.Password = _passwordHasher.HashPassword(password);
        user.UpdatedAt = now;
        _users.Update(user);

        session.ConsumedAt = now;

        await _users.SaveChangesAsync();
        await _resetSessions.SaveChangesAsync(cancellationToken);
        await _authSessions.RevokeAllForUserAsync(user.Id, "password_reset", cancellationToken);

        return new ConfirmPasswordResetResponseDTO { Message = SuccessMessage };
    }

    private static RequestPasswordResetResponseDTO BuildGenericRequestResponse(string normalizedEmail) =>
        new()
        {
            MaskedEmail = EmailMasking.MaskForDisplay(normalizedEmail),
            Message = GenericRequestMessage
        };

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail é obrigatório.");

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código é obrigatório.");

        var onlyDigits = NonDigitRegex.Replace(code, string.Empty);
        if (onlyDigits.Length != 6)
            throw new ArgumentException("Código deve conter 6 dígitos.");

        return onlyDigits;
    }

    private static string GenerateSixDigitCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashResetToken(string resetToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(resetToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildTextBody(string code, int expirationMinutes) =>
        $"""
         Use o código abaixo para redefinir sua senha na Woody. Ele expira em {expirationMinutes} minutos.

         {code}

         Se você não solicitou a redefinição de senha, ignore este e-mail.
         """;

    private static string BuildHtmlBody(string code, int expirationMinutes) =>
        $"""
         <div style="font-family: Arial, sans-serif; line-height: 1.5; color: #111827;">
           <h2 style="margin-bottom: 8px;">Redefinir senha</h2>
           <p style="margin: 0 0 12px 0;">Use o código abaixo para redefinir sua senha na Woody. Ele expira em {expirationMinutes} minutos.</p>
           <p style="font-size: 28px; font-weight: 700; letter-spacing: 6px; margin: 0 0 12px 0;">{code}</p>
           <p style="margin: 0;">Se você não solicitou a redefinição de senha, ignore este e-mail.</p>
         </div>
         """;
}
