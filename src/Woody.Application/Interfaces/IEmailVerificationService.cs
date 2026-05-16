using Woody.Application.DTOs;

namespace Woody.Application.Interfaces;

public interface IEmailVerificationService
{
    Task<SendEmailVerificationCodeResponseDTO> SendCodeAsync(
        SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<SendEmailVerificationCodeResponseDTO> ResendCodeAsync(
        SendEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<ConfirmEmailVerificationCodeResponseDTO> ConfirmCodeAsync(
        ConfirmEmailVerificationCodeRequestDTO request,
        CancellationToken cancellationToken = default);
}
