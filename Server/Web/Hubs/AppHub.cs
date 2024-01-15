using Microsoft.AspNetCore.SignalR;
using Server.Database;
using Server.Web.Extensions;

namespace Server.Web.Hubs;

public class AppHub(AppDbContext db, IServiceProvider serviceProvider) : Hub
{
    private readonly HubRouter _router = new(db, serviceProvider);

    // Version 1 of Hub API
    public async Task V1(string route, string command, string request)
    {
        var userGuid = this.GetAccountGuid(Context);
        var response = await _router.Route(route, command, request, userGuid);
        await Clients.Caller.SendAsync(command, response);
    }
}