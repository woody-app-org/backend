using System.Collections.Concurrent;

namespace Woody.Infrastructure.Repositories;

/// <summary>
/// Serializa criação de stories por usuária quando o provider não é PostgreSQL
/// (ex.: testes com SQLite). Em produção com Npgsql usa-se <c>pg_advisory_xact_lock</c>.
/// </summary>
internal static class StoryUserCreationMutex
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Gates = new();

    public static async Task<IAsyncDisposable> AcquireAsync(int userId, CancellationToken cancellationToken)
    {
        var gate = Gates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        return new Release(gate);
    }

    private sealed class Release(SemaphoreSlim gate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            gate.Release();
            return ValueTask.CompletedTask;
        }
    }
}
