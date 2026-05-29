using Moq;
using Woody.Application.Interfaces;

namespace Woody.Api.Tests;

internal static class UserBlockTestHelpers
{
    public static Mock<IUserRelationshipVisibilityService> CreateVisibilityMock()
    {
        var visibility = new Mock<IUserRelationshipVisibilityService>();
        visibility
            .Setup(v => v.AreUsersBlockedEitherWayAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        visibility
            .Setup(v => v.GetHiddenUserIdsForViewerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        return visibility;
    }
}
