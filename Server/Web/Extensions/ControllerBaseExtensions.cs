using Microsoft.AspNetCore.Mvc;

namespace Server.Web.Extensions;

public static class ControllerBaseExtensions
{
    public static Guid GetAccountGuid(this ControllerBase controller, HttpContext context)
    {
        return Guid.TryParse(context.User.Identity?.Name, out var uid) ? uid : Guid.Empty;
    }
}