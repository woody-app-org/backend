namespace Woody.Application.DTOs;

public class CheckRegistrationAvailabilityRequestDTO
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Cpf { get; set; }
}
