using Server.Core.Faucet;
using Server.Database;
using Server.Web.Models;

namespace Server.Web.Hubs.Routes;

public class FaucetRoute(AppDbContext db)
{
    private readonly FaucetManager _faucetManager = new(db);

    public async Task<HubResponse> Handle(string command, string request, Guid guid)
    {
        return command switch
        {
            "claim" => await Claim(guid),
            _ => new HubResponse()
        };
    }

    private async Task<HubResponse> Claim(Guid userGuid)
    {
        var (success, message) = await _faucetManager.Claim(userGuid);
        return new HubResponse
        {
            Success = success,
            Message = message
        };
    }
}