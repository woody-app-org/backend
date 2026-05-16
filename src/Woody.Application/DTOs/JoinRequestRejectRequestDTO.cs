namespace Woody.Application.DTOs;

public class JoinRequestRejectRequestDTO
{
    /// <summary>Motivo opcional mostrado à solicitante (máx. 500 caracteres).</summary>
    public string? Reason { get; set; }
}
