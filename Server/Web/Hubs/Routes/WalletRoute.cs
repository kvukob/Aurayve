using Server.Core.Wallets;
using Server.Database;
using Server.Web.Models;

namespace Server.Web.Hubs.Routes;

public class WalletRoute(AppDbContext db)
{
    private readonly WalletManager _walletManager = new(db);

    public async Task<HubResponse> Handle(string command, string request, Guid guid)
    {
        return command switch
        {
            "get-wallet" => await GetWallet(guid),
            _ => new HubResponse()
        };
    }

    private async Task<HubResponse> GetWallet(Guid userGuid)
    {
        var wallet = await _walletManager.Get(userGuid);

        var simplifiedWallet = new
        {
            Balances = wallet.Balances.Select(balance => new
            {
                balance.Coin.Name,
                balance.Coin.Symbol,
                balance.Quantity
            }).ToList()
        };

        var response = new HubResponse
        {
            Success = true,
            Data = simplifiedWallet
        };
        return response;
    }
}