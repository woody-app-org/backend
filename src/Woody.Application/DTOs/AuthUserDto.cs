namespace Woody.Application.DTOs;

/// <summary>Utilizadora autenticada em login/registo. <see cref="Subscription"/> espelha o estado em <c>user_subscriptions</c> para a UI.</summary>
public class AuthUserDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>Role global da utilizadora: User, Admin, SuperAdmin.</summary>
    public string Role { get; set; } = null!;

    /// <summary>Status de verificação de identidade. Usado pelo frontend para decidir redirecionamento pós-login.</summary>
    public string VerificationStatus { get; set; } = null!;

    /// <summary>
    /// Plano e ciclo de vida da assinatura. Integração futura com gateway: atualizar linhas via webhooks e devolver aqui o estado recalculado.
    /// </summary>
    public UserSubscriptionStateDto Subscription { get; set; } = null!;
}
