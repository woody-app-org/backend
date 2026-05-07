namespace Woody.Application.Interfaces;

public interface IBetaInviteRepository
{
    /// <summary>Verifica se o convite pode ser usado, sem consumir uso.</summary>
    Task<bool> IsValidForPreviewAsync(string normalizedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Incrementa atomicamente <c>UsesCount</c> se ainda houver uso disponível.
    /// Devolve o Id do convite quando uma linha foi atualizada; caso contrário null (corrida ou inválido).
    /// </summary>
    Task<int?> TryConsumeOneUseAsync(string normalizedCode, CancellationToken cancellationToken = default);
}
