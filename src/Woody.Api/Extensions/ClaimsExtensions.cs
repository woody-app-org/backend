using System.Security.Claims;

namespace Woody.Api.Extensions;

public static class ClaimsExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
