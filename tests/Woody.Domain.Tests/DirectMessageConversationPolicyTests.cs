using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Messaging;

namespace Woody.Domain.Tests;

public class DirectMessageConversationPolicyTests
{
    [Fact]
    public void OrderParticipantPair_normalizes_low_high()
    {
        Assert.Equal((1, 9), DirectMessageConversationPolicy.OrderParticipantPair(9, 1));
        Assert.Equal((1, 9), DirectMessageConversationPolicy.OrderParticipantPair(1, 9));
    }

    [Fact]
    public void OrderParticipantPair_rejects_same_user()
    {
        Assert.Throws<ArgumentException>(() => DirectMessageConversationPolicy.OrderParticipantPair(3, 3));
    }

    [Fact]
    public void InitialStatus_mutual_vs_request()
    {
        Assert.Equal(ConversationStatus.Accepted, DirectMessageConversationPolicy.InitialStatus(mutualFollow: true));
        Assert.Equal(ConversationStatus.Pending, DirectMessageConversationPolicy.InitialStatus(mutualFollow: false));
    }

    [Fact]
    public void MaySendMessage_pending_only_initiator()
    {
        var c = new Conversation
        {
            UserLowId = 1,
            UserHighId = 2,
            InitiatorUserId = 2,
            Status = ConversationStatus.Pending
        };

        Assert.True(DirectMessageConversationPolicy.MaySendMessage(c, 2));
        Assert.False(DirectMessageConversationPolicy.MaySendMessage(c, 1));
    }

    [Fact]
    public void MayAcceptOrRejectRequest_only_non_initiator()
    {
        var c = new Conversation
        {
            UserLowId = 1,
            UserHighId = 2,
            InitiatorUserId = 2,
            Status = ConversationStatus.Pending
        };

        Assert.True(DirectMessageConversationPolicy.MayAcceptOrRejectRequest(c, 1));
        Assert.False(DirectMessageConversationPolicy.MayAcceptOrRejectRequest(c, 2));
    }
}
