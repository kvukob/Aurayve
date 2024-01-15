using Microsoft.EntityFrameworkCore;
using Server.Core.Coins;
using Server.Core.Wallets;
using Server.Database;
using Server.Logging;

namespace Server.Core.Faucet;

public class FaucetManager(AppDbContext db)
{
    private const decimal ClaimAmount = 0.25m;

    public async Task<Tuple<bool, string>> Claim(Guid accountGuid)
    {
        var walletManager = new WalletManager(db);
        var wallet = await walletManager.Get(accountGuid);
        var arzBalance = wallet.Balances.FirstOrDefault(b => b.Coin.Symbol == "ARZ");
        var coin = await db.Coins.FirstOrDefaultAsync(c => c.Symbol == "ARZ");
        if (coin is null) return new Tuple<bool, string>(false, "Coin does not exist.");

        if (arzBalance is null)
        {
            arzBalance = new WalletBalance
            {
                Coin = coin,
                Quantity = ClaimAmount,
                Wallet = wallet
            };
            await db.WalletBalances.AddAsync(arzBalance);
        }
        else
        {
            arzBalance.Quantity += ClaimAmount;
            db.WalletBalances.Update(arzBalance);
        }

        await LogClaimEvent(wallet, coin, 0.25);

        return await db.SaveChangesAsync() == 2
            ? new Tuple<bool, string>(true, $"Claimed {ClaimAmount} {coin.Symbol} ")
            : new Tuple<bool, string>(false, $"Error Claiming {coin.Symbol}");
    }

    private async Task LogClaimEvent(Wallet wallet, Coin coin, double amount)
    {
        var logItem = new FaucetLog
        {
            Wallet = wallet,
            Coin = coin,
            ClaimAmount = amount,
            ClaimTime = DateTime.UtcNow
        };
        await db.FaucetLogs.AddAsync(logItem);
    }
}