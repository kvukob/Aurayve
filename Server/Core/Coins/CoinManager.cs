using Server.Database;

namespace Server.Core.Coins;

public class CoinManager(AppDbContext db)
{
    public async Task<Coin?> CreateCoin(string name, string symbol)
    {
        var coin = new Coin
        {
            Name = name,
            Symbol = symbol
        };

        await db.Coins.AddAsync(coin);
        var added = await db.SaveChangesAsync();
        return added == 1 ? coin : null;
    }
}