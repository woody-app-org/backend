using Woody.Application.Posts;

namespace Woody.Application.Tests;

public class PostPublicIdGeneratorTests
{
    [Fact]
    public void Generate_HasPrefixAndExpectedLength()
    {
        var id = PostPublicIdGenerator.Generate();
        Assert.StartsWith(PostPublicIdGenerator.Prefix, id);
        Assert.Equal(PostPublicIdGenerator.MaxLength, id.Length);
    }

    [Fact]
    public void Generate_ProducesDistinctValues()
    {
        var ids = Enumerable.Range(0, 32).Select(_ => PostPublicIdGenerator.Generate()).ToHashSet();
        Assert.Equal(32, ids.Count);
    }
}
