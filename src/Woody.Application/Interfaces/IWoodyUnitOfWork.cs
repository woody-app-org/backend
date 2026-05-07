namespace Woody.Application.Interfaces;

/// <summary>
/// Encapsula uma transação sobre o mesmo contexto de persistência dos repositórios scoped,
/// para operações que precisam de atomicidade (ex.: consumo de convite + criação de utilizadora).
/// </summary>
public interface IWoodyUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
