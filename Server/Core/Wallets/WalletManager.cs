using Microsoft.EntityFrameworkCore;
using Server.Database;

namespace Server.Core.Wallets;

public class WalletManager(AppDbContext db)
{
    private async Task<bool> Create(Guid accountGuid)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Guid == accountGuid);
        if (account is null)
            return false;
        var wallet = new Wallet { Account = account };
        await db.Wallets.AddAsync(wallet);

        // Add initial platform coin balances
        var arzBalance = new WalletBalance
        {
            Coin = await db.Coins.FirstAsync(c => c.Symbol == "ARZ"),
            Wallet = wallet
        };
        var aystBalance = new WalletBalance
        {
            Coin = await db.Coins.FirstAsync(c => c.Symbol == "AYST"),
            Wallet = wallet
        };
        await db.WalletBalances.AddAsync(arzBalance);
        await db.WalletBalances.AddAsync(aystBalance);
        var created = await db.SaveChangesAsync();
        return true;
    }

    public async Task<Wallet> Get(Guid accountGuid)
    {
        // Get account wallet, create it if doesn't exist
        var wallet = await db.Wallets
            .Include(w => w.Balances)
            .ThenInclude(b => b.Coin)
            .FirstOrDefaultAsync(w => w.Account.Guid == accountGuid);
        if (wallet is not null) return wallet;
        // Create wallet if it doesn't exist
        var created = await Create(accountGuid);
        // Fetch created wallet to include balances
        if (created)
            wallet = await Get(accountGuid);
        return wallet!;
    }
}