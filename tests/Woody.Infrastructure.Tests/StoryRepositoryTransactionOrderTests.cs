namespace Woody.Infrastructure.Tests;

/// <summary>
/// Garante que a ordem documentada de transação/lock no repositório não regride silenciosamente.
/// </summary>
public class StoryRepositoryTransactionOrderTests
{
    [Fact]
    public void CreateWithActiveLimitAsync_SourceOrder_IsTransactionLockCountInsertCommit()
    {
        var source = ReadRepositorySource();
        var methodStart = source.IndexOf("CreateWithActiveLimitAsync", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);

        var methodEnd = source.IndexOf("public async Task<bool> SoftDeleteAsync", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);

        var methodBody = source[methodStart..methodEnd];

        AssertOrdered(methodBody, "BeginTransactionAsync", "AcquirePgCreationLockAsync");
        AssertOrdered(methodBody, "AcquirePgCreationLockAsync", "CountActiveStoriesAsync");
        AssertOrdered(methodBody, "CountActiveStoriesAsync", "Stories.Add");
        AssertOrdered(methodBody, "SaveChangesAsync", "CommitAsync");
    }

    [Fact]
    public void AcquirePgCreationLock_UsesXactLockPerUserId()
    {
        var source = ReadRepositorySource();
        Assert.Contains("pg_advisory_xact_lock", source, StringComparison.Ordinal);
        Assert.Contains("IsNpgsql()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DoesNotUsePartialIndexWithNow()
    {
        var migrationPath = ResolveMigrationPath();
        Assert.True(File.Exists(migrationPath), $"Migration não encontrada: {migrationPath}");

        var content = File.ReadAllText(migrationPath);
        Assert.DoesNotContain("NOW()", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deleted_at IS NULL", content, StringComparison.Ordinal);
        Assert.Contains("ix_stories_author_expires_not_deleted", content, StringComparison.Ordinal);
    }

    private static void AssertOrdered(string text, string first, string second)
    {
        var i = text.IndexOf(first, StringComparison.Ordinal);
        var j = text.IndexOf(second, StringComparison.Ordinal);
        Assert.True(i >= 0, $"Trecho não encontrado: {first}");
        Assert.True(j >= 0, $"Trecho não encontrado: {second}");
        Assert.True(i < j, $"Esperado '{first}' antes de '{second}'.");
    }

    private static string ReadRepositorySource()
    {
        var path = ResolveRepositoryPath();
        Assert.True(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string ResolveRepositoryPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Woody.Infrastructure", "Repositories", "StoryRepository.cs"));

    private static string ResolveMigrationPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Woody.Infrastructure", "Migrations", "20260518233425_AddStories.cs"));
}
