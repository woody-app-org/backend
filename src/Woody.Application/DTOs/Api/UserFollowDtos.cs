namespace Woody.Application.DTOs.Api;

/// <summary>
/// Estado de follow de um perfil para consumo no cliente (botão seguir, contagens).
/// </summary>
public class UserFollowStatusResponseDto
{
    public string TargetUserId { get; set; } = null!;

    /// <summary>
    /// Null se não há utilizador autenticado ou se o alvo é o próprio visitante.
    /// </summary>
    public bool? IsFollowing { get; set; }

    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}

/// <summary>
/// Resposta após seguir / deixar de seguir (estado atual e contagem de seguidores do alvo).
/// </summary>
public class FollowMutationResponseDto
{
    public bool IsFollowing { get; set; }
    public int FollowersCount { get; set; }
}
