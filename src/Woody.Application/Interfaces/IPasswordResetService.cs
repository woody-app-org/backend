using Woody.Application.DTOs;

namespace Woody.Application.Interfaces;

public interface IPasswordResetService
{
    Task<RequestPasswordResetResponseDTO> RequestAsync(
        RequestPasswordResetRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<VerifyPasswordResetCodeResponseDTO> VerifyCodeAsync(
        VerifyPasswordResetCodeRequestDTO request,
        CancellationToken cancellationToken = default);

    Task<ConfirmPasswordResetResponseDTO> ConfirmAsync(
        ConfirmPasswordResetRequestDTO request,
        CancellationToken cancellationToken = default);
}
