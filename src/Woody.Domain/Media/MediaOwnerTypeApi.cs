using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Media;

public static class MediaOwnerTypeApi
{
    public const string Post = "post";
    public const string Message = "message";

    public static string ToApiString(MediaOwnerType t) =>
        t == MediaOwnerType.Message ? Message : Post;

    public static bool TryParse(string? raw, out MediaOwnerType ownerType)
    {
        ownerType = MediaOwnerType.Post;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        switch (raw.Trim().ToLowerInvariant())
        {
            case Post:
                ownerType = MediaOwnerType.Post;
                return true;
            case Message:
                ownerType = MediaOwnerType.Message;
                return true;
            default:
                return false;
        }
    }
}
