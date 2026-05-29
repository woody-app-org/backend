using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Email;
using Woody.Application.Interfaces.Security;
using Woody.Domain.Entities;

using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private static readonly Regex NonDigitRegex = new(@"\D", RegexOptions.Compiled);

    private readonly IUserRepository _users;
    private readonly IEmailVerificationCodeRepository _codes;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly EmailVerificationOptions _options;

    public EmailVerificationService(
        IUserRepository users,
        IEmailVerificationCodeRepository codes,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IOptions<EmailVerificationOptions> options)
    {
        _users = users;
        _codes = codes;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _options = options.Value;
    }

    public Task<SendEmailVerificationCodeResponseDTO> SendCodeAsync(
        SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken = default) =>
        SendCodeInternalAsync(request, cancellationToken);

    public Task<SendEmailVerificationCodeResponseDTO> ResendCodeAsync(
        SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken = default) =>
        SendCodeInternalAsync(request, cancellationToken);

    public async Task<ConfirmEmailVerificationCodeResponseDTO> ConfirmCodeAsync(
        ConfirmEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var code = NormalizeCode(request.Code);
        var user = await _users.GetByEmailAsync(email);
        if (user is not null && user.IsEmailVerified)
            throw new ArgumentException("Código inválido.");

        var now = DateTime.UtcNow;
        var latestCode = await _codes.GetLatestByEmailAndPurposeAsync(
            email,
            VerificationCodePurpose.EmailConfirmation,
            cancellationToken);
        if (latestCode is null)
            throw new ArgumentException("Código inválido.");

        if (latestCode.ConsumedAt.HasValue)
            throw new InvalidOperationException("Código já utilizado.");

        if (latestCode.InvalidatedAt.HasValue)
            throw new ArgumentException("Código inválido.");

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
            throw new ArgumentException("Código inválido.");
        }

        latestCode.ConsumedAt = now;
        latestCode.UpdatedAt = now;
        if (user is not null)
        {
            user.IsEmailVerified = true;
            user.EmailVerifiedAt = now;
            user.UpdatedAt = now;
            latestCode.UserId = user.Id;
        }

        await _codes.SaveChangesAsync(cancellationToken);

        return new ConfirmEmailVerificationCodeResponseDTO
        {
            Verified = true,
            VerifiedAt = now
        };
    }

    private async Task<SendEmailVerificationCodeResponseDTO> SendCodeInternalAsync(
        SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email);
        if (user is not null && user.IsEmailVerified)
            return BuildGenericSendResponse();

        var now = DateTime.UtcNow;
        await _codes.InvalidateActiveByEmailAndPurposeAsync(
            email,
            VerificationCodePurpose.EmailConfirmation,
            now,
            cancellationToken);

        var plainCode = GenerateSixDigitCode();
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var entity = new EmailVerificationCode
        {
            Purpose = VerificationCodePurpose.EmailConfirmation,
            Email = email,
            UserId = user?.Id,
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
                    Subject = "Confirme seu e-mail na Woody",
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
            throw new InvalidOperationException("Não foi possível enviar o código de verificação. Tente novamente.");
        }

        return new SendEmailVerificationCodeResponseDTO
        {
            RequestId = entity.Id.ToString(),
            ExpiresAt = expiresAt
        };
    }

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

    private SendEmailVerificationCodeResponseDTO BuildGenericSendResponse()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        return new SendEmailVerificationCodeResponseDTO
        {
            RequestId = Guid.NewGuid().ToString(),
            ExpiresAt = expiresAt
        };
    }

    private static string BuildTextBody(string code, int expirationMinutes) =>
        $"Seu código de verificação da Woody é: {code}. Este código expira em {expirationMinutes} minutos.";

    private static string BuildHtmlBody(string code, int expirationMinutes) =>
        $"""
         <div style="font-family: Arial, sans-serif; line-height: 1.5; color: #111827;">
           <h2 style="margin-bottom: 8px;">Confirme seu e-mail</h2>
           <p style="margin: 0 0 12px 0;">Use o código abaixo para confirmar seu e-mail na Woody:</p>
           <p style="font-size: 28px; font-weight: 700; letter-spacing: 6px; margin: 0 0 12px 0;">{code}</p>
           <p style="margin: 0;">Este código expira em {expirationMinutes} minutos.</p>
         </div>
         """;
}
