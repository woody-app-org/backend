namespace Woody.Application.DTOs;

public class RequestPasswordResetResponseDTO
{
    public string MaskedEmail { get; set; } = null!;
    public string Message { get; set; } = null!;
}
