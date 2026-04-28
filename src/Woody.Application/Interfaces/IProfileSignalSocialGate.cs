namespace Woody.Application.Interfaces;

/// <summary>
/// Extensível para bloqueios entre utilizadoras. Implementação por defeito não bloqueia nada.
/// </summary>
public interface IProfileSignalSocialGate
{
    /// <summary>
    /// Verdadeiro se qualquer uma bloqueou a outra (quando existir tabela/serviço de bloqueios).
    /// </summary>
    Task<bool> AreUsersBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default);
}
