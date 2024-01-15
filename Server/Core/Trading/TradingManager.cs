using Microsoft.EntityFrameworkCore;
using Server.Core.Wallets;
using Server.Database;
using Server.Logging;

namespace Server.Core.Trading;

public class TradingManager(AppDbContext db)
{
    private const decimal FeePercentage = 0.02m;
    private readonly WalletManager _walletManager = new(db);

    public async Task<Pool?> GetPoolByGuid(Guid guid)
    {
        return await db.Pools
            .Include(pool => pool.PrimaryCoin)
            .Include(pool => pool.SecondaryCoin)
            .Include(pool => pool.LiquidityCoin)
            .FirstOrDefaultAsync(pool => pool.Guid == guid);
    }

    public async Task<IEnumerable<Pool>> GetPools()
    {
        return await db.Pools
            .Include(lP => lP.PrimaryCoin)
            .Include(lP => lP.SecondaryCoin)
            .ToListAsync();
    }

    public async Task<IEnumerable<PoolTradeLog>> GetPoolTrades(Guid poolGuid)
    {
        var recentTrades =
            await db.PoolTradeLogs.Where(log => log.Pool.Guid == poolGuid).OrderByDescending(log => log.Time).Take(50)
                .ToListAsync();
        return recentTrades;
    }
    
    
    public async Task<IEnumerable<object>> GetChartData(Guid poolGuid)
    {
        var recentTrades =
            await db.PoolTradeLogs.Where(log => log.Pool.Guid == poolGuid).OrderBy(log => log.Time)
                .ToListAsync();
        
        var groupedByHour = recentTrades
            .GroupBy(item => item.Time.Hour);

        // Remap the array
        var remappedArray = new List<object>();

        foreach (var group in groupedByHour)
        {
            // Calculate open, close, high, low for each group
            var openPrice = group.First().Price;
            var closePrice = group.Last().Price;
            var highPrice = group.Max(item => item.Price);
            var lowPrice = group.Min(item => item.Price);

            // Map to the new format
            var mappedItem = new
            {
                x = group.First().Time,
                y = new [] { openPrice, highPrice, lowPrice, closePrice }
                
            };

            // Add to the remapped array
            remappedArray.Add(mappedItem);
        }

        return remappedArray;
    }

    public async Task<bool> AddLiquidity(Guid accountGuid, Guid poolGuid, decimal primaryQuantity,
        decimal secondaryQuantity)
    {
        var wallet = await _walletManager.Get(accountGuid);

        var pool = await db.Pools
            .Include(lP => lP.PrimaryCoin)
            .Include(lP => lP.SecondaryCoin)
            .Include(lP => lP.LiquidityCoin)
            .FirstOrDefaultAsync(p => p.Guid == poolGuid);
        if (pool is null) return false; // Pool doesn't exist

        var primaryBalance = wallet.GetBalance(pool.PrimaryCoin);
        var secondaryBalance = wallet.GetBalance(pool.SecondaryCoin);
        if (primaryBalance is null || secondaryBalance is null)
            return false; // Not owned

        if (primaryBalance.Quantity < primaryQuantity ||
            secondaryBalance.Quantity < secondaryQuantity) return false; // Not enough

        pool.PooledPrimaryCoin += primaryQuantity;
        pool.PooledSecondaryCoin += secondaryQuantity;
        db.Pools.Update(pool);

        primaryBalance.Quantity -= primaryQuantity;
        secondaryBalance.Quantity -= secondaryQuantity;

        var lpBalance = wallet.Balances.FirstOrDefault(b => b.Coin == pool.LiquidityCoin);

        if (lpBalance is null)
        {
            lpBalance = new WalletBalance
            {
                Coin = pool.LiquidityCoin,
                Quantity = CalculateLpCoins(pool, primaryQuantity, secondaryQuantity),
                Wallet = wallet
            };
            await db.WalletBalances.AddAsync(lpBalance);
            await db.SaveChangesAsync();
            return true;
        }

        lpBalance.Quantity += CalculateLpCoins(pool, primaryQuantity, secondaryQuantity);
        db.WalletBalances.UpdateRange(lpBalance, primaryBalance, secondaryBalance);


        await db.SaveChangesAsync();

        return true;
    }

    private decimal CalculateLpCoins(Pool pool, decimal primaryQuantity, decimal secondaryQuantity)
    {
        var amount = (decimal)Math.Sqrt((double)primaryQuantity * (double)secondaryQuantity);
        return amount;
    }

    public async Task<bool> BuyCoins(Guid accountGuid, Guid poolGuid, decimal quantitySold)
    {
        var pool = await GetPoolByGuid(poolGuid);
        if (pool is null) return false;
        // Get users wallet
        var wallet = await _walletManager.Get(accountGuid);

        // Get balances for pool coins
        var baseBalance = wallet.GetBalance(pool.PrimaryCoin);
        var quoteBalance = wallet.GetBalance(pool.SecondaryCoin);
        if (quoteBalance is null) // Doesnt have needed coin to buy with
            return false;
        if (baseBalance is null)
            baseBalance = new WalletBalance
            {
                Coin = pool.PrimaryCoin,
                Wallet = wallet
            };

        // Insufficient funds
        if (quantitySold > quoteBalance.Quantity)
            return false;

        var feeAmount = quantitySold * FeePercentage;
        //TODO 
        //TODO Send fee amount to liquidity providers
        //TODO 


        // AMMM k = x * y

        var dBeta = quantitySold - feeAmount;
        var k = pool.PooledPrimaryCoin * pool.PooledSecondaryCoin;
        var rAlpha = pool.PooledPrimaryCoin;
        var rBeta = pool.PooledSecondaryCoin;
        var dAlpha = k / (rBeta + dBeta);
        var received = rAlpha - dAlpha;


        // Ensure pool has coins
        if (received >= pool.PooledPrimaryCoin) return false;

        // Add the purchased coins
        baseBalance.Quantity += received;

        // Remove the sold coins
        quoteBalance.Quantity -= quantitySold;

        //Update liquidity pool
        pool.PooledPrimaryCoin -= received;
        pool.PooledSecondaryCoin += quantitySold - feeAmount;

        db.Pools.Update(pool);
        db.WalletBalances.UpdateRange(baseBalance, quoteBalance);

        // Log trade event
        var poolLog = new PoolTradeLog
        {
            TradeType = TradeType.Buy,
            Time = DateTime.UtcNow,
            Price = pool.PooledSecondaryCoin / pool.PooledPrimaryCoin,
            QuantityReceived = received,
            CoinReceived = baseBalance.Coin,
            Pool = pool,
            Wallet = wallet
        };
        await db.PoolTradeLogs.AddAsync(poolLog);

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SellCoins(Guid accountGuid, Guid poolGuid, decimal quantitySold)
    {
        var pool = await GetPoolByGuid(poolGuid);
        if (pool is null) return false;
        // Get users wallet
        var wallet = await _walletManager.Get(accountGuid);

        // Get balances for pool coins
        var baseBalance = wallet.GetBalance(pool.PrimaryCoin);
        var quoteBalance = wallet.GetBalance(pool.SecondaryCoin);
        if (baseBalance is null)
            return false;
        if (quoteBalance is null)
            quoteBalance = new WalletBalance
            {
                Coin = pool.SecondaryCoin,
                Wallet = wallet
            };

        var feeAmount = quantitySold * FeePercentage;
        //TODO 
        //TODO Send fee amount to liquidity providers
        //TODO 

        // AMMM k = x * y
        var dAlpha = quantitySold - feeAmount;
        var k = pool.PooledPrimaryCoin * pool.PooledSecondaryCoin;
        var rAlpha = pool.PooledPrimaryCoin;
        var rBeta = pool.PooledSecondaryCoin;
        var dBeta = k / (rAlpha - dAlpha);
        var received = dBeta - rBeta;

        // Ensure pool has coins
        if (received >= pool.PooledSecondaryCoin) return false;

        // Add the purchased coins
        baseBalance.Quantity -= quantitySold;

        // Remove the sold coins
        quoteBalance.Quantity += received;

        //Update liquidity pool
        pool.PooledPrimaryCoin += quantitySold - feeAmount;
        pool.PooledSecondaryCoin -= received;

        db.Pools.Update(pool);
        db.WalletBalances.UpdateRange(baseBalance, quoteBalance);

        // Log trade event
        var poolLog = new PoolTradeLog
        {
            TradeType = TradeType.Sell,
            Time = DateTime.UtcNow,
            Price = pool.PooledSecondaryCoin / pool.PooledPrimaryCoin,
            QuantityReceived = quantitySold,
            CoinReceived = quoteBalance.Coin,
            Pool = pool,
            Wallet = wallet
        };
        await db.PoolTradeLogs.AddAsync(poolLog);

        await db.SaveChangesAsync();

        return true;
    }
    

}