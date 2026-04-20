namespace Woody.Api.Hubs;

public static class DirectMessageHubGroups
{
    public static string Conversation(int conversationId) => $"dm:c:{conversationId}";

    public static string UserInbox(int userId) => $"dm:u:{userId}";
}
