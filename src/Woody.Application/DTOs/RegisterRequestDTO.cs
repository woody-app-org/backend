namespace Woody.Application.DTOs;

public class RegisterRequestDTO
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Cpf { get; set; } = null!;
    /// <summary>ISO YYYY-MM-DD</summary>
    public string BirthDate { get; set; } = null!;
    public string? AvatarUrl { get; set; }

    /// <summary>Obrigatório quando o beta fechado está ativo no servidor.</summary>
    public string? InviteCode { get; set; }
}
