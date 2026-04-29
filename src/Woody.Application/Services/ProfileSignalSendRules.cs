using Woody.Domain.Entities.Enum;

namespace Woody.Application.Services;

/// <summary>
/// Regras puras de elegibilidade (preferência de entrada + grafo social). Anti-spam/cooldown fica no serviço.
/// </summary>
public static class ProfileSignalSendRules
{
    /// <param name="senderFollowsReceiver"><c>Follow</c> em que remetente segue destinatária.</param>
    /// <param name="receiverFollowsSender"><c>Follow</c> em que destinatária segue remetente.</param>
    public static bool MeetsIncomingPreference(
        ProfileSignalsIncomingPreference preference,
        bool senderFollowsReceiver,
        bool receiverFollowsSender) =>
        preference switch
        {
            ProfileSignalsIncomingPreference.All => true,
            ProfileSignalsIncomingPreference.Nobody => false,
            ProfileSignalsIncomingPreference.FollowingOnly => receiverFollowsSender,
            ProfileSignalsIncomingPreference.FollowersOnly => senderFollowsReceiver,
            _ => false
        };

    public static bool TryParseIncomingPreference(string? raw, out ProfileSignalsIncomingPreference preference)
    {
        preference = ProfileSignalsIncomingPreference.All;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "all":
                preference = ProfileSignalsIncomingPreference.All;
                return true;
            case "following_only":
                preference = ProfileSignalsIncomingPreference.FollowingOnly;
                return true;
            case "followers_only":
                preference = ProfileSignalsIncomingPreference.FollowersOnly;
                return true;
            case "nobody":
                preference = ProfileSignalsIncomingPreference.Nobody;
                return true;
            default:
                return false;
        }
    }

    public static string ToApiString(ProfileSignalsIncomingPreference preference) =>
        preference switch
        {
            ProfileSignalsIncomingPreference.All => "all",
            ProfileSignalsIncomingPreference.FollowingOnly => "following_only",
            ProfileSignalsIncomingPreference.FollowersOnly => "followers_only",
            ProfileSignalsIncomingPreference.Nobody => "nobody",
            _ => "all"
        };
}
