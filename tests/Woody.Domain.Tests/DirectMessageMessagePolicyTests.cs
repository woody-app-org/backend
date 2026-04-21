using Woody.Domain.Entities;
using Woody.Domain.Messaging;

namespace Woody.Domain.Tests;

public class DirectMessageMessagePolicyTests
{
    [Fact]
    public void MayEditOrSoftDelete_only_author_not_deleted()
    {
        var m = new Message { SenderUserId = 2, DeletedAt = null };
        Assert.True(DirectMessageMessagePolicy.MayEditOrSoftDelete(m, 2));
        Assert.False(DirectMessageMessagePolicy.MayEditOrSoftDelete(m, 1));
        m.DeletedAt = DateTime.UtcNow;
        Assert.False(DirectMessageMessagePolicy.MayEditOrSoftDelete(m, 2));
    }
}
