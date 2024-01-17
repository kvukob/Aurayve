using Microsoft.EntityFrameworkCore;
using Server.Core.Coins;
using Server.Core.Wallets;
using Server.Database;
using Server.Logging;

namespace Server.Core.Faucet;

public class FaucetManager(AppDbContext db)
{
    private const decimal ClaimAmount = 0.25m;

    /// <summary>
    ///     Claims coins out of a specified faucet.
    /// </summary>
    /// <param name="accountGuid">The <see cref="Guid" /> identifying the account.</param>
    /// <returns>
    ///     A Tuple DTO containing whether the claim was successful or not with a message.
    /// </returns>
    public async Task<Tuple<bool, string>> Claim(Guid accountGuid)
    {
        var walletManager = new WalletManager(db);
        var coin = await db.Coins.FirstOrDefaultAsync(c => c.Symbol == "ARZ");
        if (coin is null) return new Tuple<bool, string>(false, "Server error when attempting to claim ");

        var wallet = await walletManager.Get(accountGuid);

        wallet.DepositCoin(coin, ClaimAmount);
        await LogClaimEvent(wallet, coin, ClaimAmount);

        return await db.SaveChangesAsync() >= 2
            ? new Tuple<bool, string>(true, $" You claimed {ClaimAmount} {coin.Symbol}!")
            : new Tuple<bool, string>(false, $"Error claiming {coin.Symbol}.  Please contact support.");
    }

    /// <summary>
    ///     Logs a faucet claim event to the database.
    /// </summary>
    /// <param name="wallet">The <see cref="Guid" /> identifying the wallet that claimed.</param>
    /// <param name="coin">The <see cref="Guid" /> identifying the claimed coin.</param>
    /// <param name="amount">The <see cref="Guid" /> identifying the claimed amount.</param>
    private async Task LogClaimEvent(Wallet wallet, Coin coin, decimal amount)
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