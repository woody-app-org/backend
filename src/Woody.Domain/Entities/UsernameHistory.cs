namespace Woody.Domain.Entities;

/// <summary>
/// Histórico de alterações de username para redirects futuros (<c>/profile/old</c> → <c>/profile/new</c>).
/// </summary>
public class UsernameHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string OldUsername { get; set; } = null!;
    public string NewUsername { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
}
