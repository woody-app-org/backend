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
    public async Task GetUnreadReceivedCount_DelegatesToRepository()
    {
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.CountUnreadReceivedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var service = CreateService(signals: signals);

        var result = await service.GetUnreadReceivedCountAsync(10, CancellationToken.None);

        Assert.Equal(3, result.UnreadCount);
        signals.Verify(x => x.CountUnreadReceivedAsync(10, It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task MarkRead_OnlyReceiverCanReadAndSetsReadTimestamp()
    {
        var signal = TestSignal(receiverUserId: 10, status: ProfileSignalStatus.Sent);
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.GetByIdWithUsersAsync(signal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(signal);

        var service = CreateService(signals: signals);

        var result = await service.MarkReadAsync(10, signal.Id, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.Success, result.Outcome);
        Assert.Equal(ProfileSignalStatus.Read, signal.Status);
        Assert.NotNull(signal.ReadAt);
        signals.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Archive_OnlyReceiverCanArchiveAndSetsArchiveTimestamp()
    {
        var signal = TestSignal(receiverUserId: 10, status: ProfileSignalStatus.Read);
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.GetByIdWithUsersAsync(signal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(signal);

        var service = CreateService(signals: signals);

        var result = await service.ArchiveAsync(10, signal.Id, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.Success, result.Outcome);
        Assert.Equal(ProfileSignalStatus.Archived, signal.Status);
        Assert.NotNull(signal.ArchivedAt);
        signals.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dismiss_OnlyReceiverCanDismissAndSetsDismissedTimestamp()
    {
        var signal = TestSignal(receiverUserId: 10, status: ProfileSignalStatus.Sent);
        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.GetByIdWithUsersAsync(signal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(signal);

        var service = CreateService(signals: signals);

        var result = await service.DismissAsync(10, signal.Id, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.Success, result.Outcome);
        Assert.Equal(ProfileSignalStatus.Dismissed, signal.Status);
        Assert.NotNull(signal.DismissedAt);
        signals.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_ReturnsLabelAndEmojiMetadata()
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(x => x.GetByIdNoTrackingAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 20, Username = "bia", Email = "bia@example.com", Role = "User" });

        var signals = new Mock<IProfileSignalRepository>();
        signals
            .Setup(x => x.HasSentTypeSinceAsync(10, 20, ProfileSignalType.SinalVerde, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        signals
            .Setup(x => x.GetByIdWithUsersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestSignal(receiverUserId: 20, status: ProfileSignalStatus.Sent, type: ProfileSignalType.SinalVerde));

        var service = CreateService(signals, users);

        var result = await service.SendAsync(10, 20, "sinal_verde", null, CancellationToken.None);

        Assert.Equal(ProfileSignalOperationOutcome.Success, result.Outcome);
        Assert.Equal("sinal_verde", result.Signal?.Type);
        Assert.Equal("Sinal verde", result.Signal?.Label);
        Assert.Equal("✅", result.Signal?.Emoji);
    }

    private static ProfileSignalService CreateService(
        Mock<IProfileSignalRepository>? signals = null,
        Mock<IUserRepository>? users = null)
        => new(
            (signals ?? new Mock<IProfileSignalRepository>()).Object,
            (users ?? new Mock<IUserRepository>()).Object);

    private static ProfileSignal TestSignal(
        int receiverUserId,
        ProfileSignalStatus status,
        ProfileSignalType type = ProfileSignalType.TeNotei) =>
        new()
        {
            Id = 5,
            SenderUserId = 20,
            ReceiverUserId = receiverUserId,
            Type = type,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            SenderUser = new User { Id = 20, Username = "bia", Email = "bia@example.com", Role = "User" },
            ReceiverUser = new User { Id = receiverUserId, Username = "lia", Email = "lia@example.com", Role = "User" }
        };
}
