using Moq;
using Woody.Application.Interfaces;
using Woody.Application.Services;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Tests;

public class ProfileSignalsControllerTests
{
    [Fact]
    public async Task Send_RejectsSelfSignalBeforePersistence()
    {
        var signals = new Mock<IProfileSignalRepository>();
        var service = CreateService(signals: signals);

        var result = await service.SendAsync(10, 10, "te_notei", null, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.SelfSignal, result.Outcome);
        signals.Verify(x => x.Add(It.IsAny<ProfileSignal>()), Times.Never);
        signals.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Send_RejectsInvalidTypeBeforePersistence()
    {
        var signals = new Mock<IProfileSignalRepository>();
        var service = CreateService(signals: signals);

        var result = await service.SendAsync(10, 20, "banana", null, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.InvalidType, result.Outcome);
        signals.Verify(x => x.Add(It.IsAny<ProfileSignal>()), Times.Never);
    }

    [Fact]
    public async Task Send_RejectsWhenSameTypeForPairIsInsideCooldown()
    {
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.HasSentTypeSinceAsync(10, 20, ProfileSignalType.Olhadinha, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        signals
            .Setup(x => x.GetLatestOfTypeBetweenAsync(10, 20, ProfileSignalType.Olhadinha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileSignal
            {
                Id = 1,
                SenderUserId = 10,
                ReceiverUserId = 20,
                Type = ProfileSignalType.Olhadinha,
                CreatedAt = DateTime.UtcNow
            });

        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdNoTrackingAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 20, Username = "bia", Email = "bia@example.com", Role = "User" });

        var service = CreateService(signals, users);

        var result = await service.SendAsync(10, 20, "olhadinha", null, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.CooldownActive, result.Outcome);
        signals.Verify(x => x.Add(It.IsAny<ProfileSignal>()), Times.Never);
    }

    [Fact]
    public async Task ListReceived_UsesReceiverUserIdOnly()
    {
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.ListReceivedInboxPagedAsync(10, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ProfileSignal>(), 0));

        var service = CreateService(signals: signals);

        var result = await service.ListReceivedAsync(10, 1, 20, CancellationToken.None);

        Assert.Empty(result.Items);
        signals.Verify(x => x.ListReceivedInboxPagedAsync(10, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
        signals.Verify(x => x.ListReceivedInboxPagedAsync(20, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Archive_ForbidsUserWhoIsNotReceiver()
    {
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.GetByIdWithUsersAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileSignal
            {
                Id = 5,
                SenderUserId = 20,
                ReceiverUserId = 30,
                Type = ProfileSignalType.TeNotei,
                Status = ProfileSignalStatus.Sent,
                CreatedAt = DateTime.UtcNow,
                SenderUser = new User { Id = 20, Username = "bia", Email = "bia@example.com", Role = "User" },
                ReceiverUser = new User { Id = 30, Username = "lia", Email = "lia@example.com", Role = "User" }
            });

        var service = CreateService(signals: signals);

        var result = await service.ArchiveAsync(10, 5, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.Forbidden, result.Outcome);
        signals.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ProfileSignalService CreateService(
        Mock<IProfileSignalRepository>? signals = null,
        Mock<IUserRepository>? users = null)
        => new(
            (signals ?? new Mock<IProfileSignalRepository>()).Object,
            (users ?? new Mock<IUserRepository>()).Object);
}
