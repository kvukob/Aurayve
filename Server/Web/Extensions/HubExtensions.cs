using Microsoft.AspNetCore.SignalR;

namespace Server.Web.Extensions;

public static class HubExtensions
{
    // Gets an account ID from hub api caller.
    public static Guid GetAccountGuid(this Hub hub, HubCallerContext context)
    {
        return Guid.TryParse(context.User?.Identity?.Name, out var guid) ? guid : Guid.Empty;
    }
}