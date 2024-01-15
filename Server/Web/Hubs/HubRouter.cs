using Server.Database;
using Server.Web.Hubs.Routes;
using Server.Web.Hubs.Routes.Trade;
using Server.Web.Models;

namespace Server.Web.Hubs;

public class HubRouter(AppDbContext db, IServiceProvider serviceProvider)
{
    public async Task<HubResponse> Route(string route, string command, string request, Guid guid)
    {
        return route switch
        {
            HubRoutes.Wallet => await new WalletRoute(db).Handle(command, request, guid),
            HubRoutes.Faucet => await new FaucetRoute(db).Handle(command, request, guid),
            HubRoutes.Trade => await new TradeRoute(db).Handle(command, request, guid),
            _ => new HubResponse { Success = false }
        };
    }
}