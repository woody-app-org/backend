namespace Woody.Application.DTOs;

public class LoginRequestDTO
{
    /// <summary>Nome de utilizador (preferido pelo frontend).</summary>
    public string? Username { get; set; }
    /// <summary>Alternativa: email.</summary>
    public string? Email { get; set; }
    public string Password { get; set; } = null!;
}
