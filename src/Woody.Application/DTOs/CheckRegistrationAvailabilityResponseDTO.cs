namespace Woody.Application.DTOs;

public class CheckRegistrationAvailabilityResponseDTO
{
    public FieldAvailabilityDTO? Username { get; set; }
    public FieldAvailabilityDTO? Email { get; set; }
    public FieldAvailabilityDTO? Cpf { get; set; }
}

public class FieldAvailabilityDTO
{
    public bool Available { get; set; }
    public string? Message { get; set; }
}
