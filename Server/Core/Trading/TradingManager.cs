using Microsoft.EntityFrameworkCore;
using Server.Core.Wallets;
using Server.Database;
using Server.Logging;

namespace Server.Core.Trading;

public class TradingManager(AppDbContext db)
{
    private const decimal FeePercentage = 0.02m;
    private readonly WalletManager _walletManager = new(db);

    /// <summary>
    ///     Gets a pools information from a specific Guid.
    /// </summary>
    /// <param name="poolGuid">The <see cref="Guid" /> identifying the pool.</param>
    /// <returns>
    ///     A <see cref="Pool" /> object containing pool information.
    /// </returns>
    public async Task<Pool?> GetPoolByGuid(Guid poolGuid)
    {
        return await db.Pools
            .Include(pool => pool.PrimaryCoin)
            .Include(pool => pool.SecondaryCoin)
            .Include(pool => pool.LiquidityCoin)
            .FirstOrDefaultAsync(pool => pool.Guid == poolGuid);
    }

    /// <summary>
    ///     Gets all available pools
    /// </summary>
    /// <returns>
    ///     A list of <see cref="Pool" />s.
    /// </returns>
    public async Task<IEnumerable<Pool>> GetPools()
    {
        return await db.Pools
            .Include(lP => lP.PrimaryCoin)
            .Include(lP => lP.SecondaryCoin)
            .ToListAsync();
    }

    /// <summary>
    ///     Gets a pools trade history.
    /// </summary>
    /// <param name="poolGuid">The <see cref="Guid" /> identifying the pool.</param>
    /// <returns>
    ///     A list of <see cref="PoolTradeLog" />s containing pool trade history.
    /// </returns>
    public async Task<IEnumerable<PoolTradeLog>> GetPoolTrades(Guid poolGuid)
    {
        var recentTrades =
            await db.PoolTradeLogs
                .Where(log => log.Pool.Guid == poolGuid)
                .OrderByDescending(log => log.Time)
                .Take(50)
                .ToListAsync();
        return recentTrades;
    }

    /// <summary>
    ///     Gets a pools chart data.
    /// </summary>
    /// <param name="poolGuid">The <see cref="Guid" /> identifying the pool.</param>
    /// <returns>
    ///     A object containing candlestick chart data for the specified pool.
    /// </returns>
    public async Task<IEnumerable<object>> GetChartData(Guid poolGuid)
    {
        var recentTrades =
            await db.PoolTradeLogs
                .Where(log => log.Pool.Guid == poolGuid)
                .OrderBy(log => log.Time)
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
                y = new[] { openPrice, highPrice, lowPrice, closePrice }
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

    /// <summary>
    ///     Processes a buy order into the specified pool.
    /// </summary>
    /// <param name="accountGuid">The <see cref="Guid" /> identifying the account.</param>
    /// <param name="poolGuid">The <see cref="Guid" /> identifying the pool.</param>
    /// <param name="quantitySold">The quantity of quote asset to be sold. </param>
    /// <returns>
    ///     A boolean indication trade success or failure.
    /// </returns>
    public async Task<bool> BuyCoins(Guid accountGuid, Guid poolGuid, decimal quantitySold)
    {
        var pool = await GetPoolByGuid(poolGuid);
        if (pool is null) return false;
        // Get users wallet
        var wallet = await _walletManager.Get(accountGuid);

        // Return false if wallet doesnt have required coins
        if (!wallet.CheckBalance(pool.SecondaryCoin, quantitySold))
            return false;

        var feeAmount = quantitySold * FeePercentage;
        //TODO 
        //TODO Send fee amount to liquidity providers
        //TODO 

        var coinsReceived = CalculateReceivedOnBuy(pool, quantitySold);

        // Ensure pool has coins
        if (coinsReceived <= 0) return false;
        if (coinsReceived >= pool.PooledPrimaryCoin) return false;

        // Add the purchased coins
        wallet.DepositCoin(pool.PrimaryCoin, coinsReceived);

        // Remove the sold coins
        wallet.WithdrawCoin(pool.SecondaryCoin, quantitySold);

        //Update liquidity pool
        pool.PooledPrimaryCoin -= coinsReceived;
        pool.PooledSecondaryCoin += quantitySold - feeAmount;

        db.Pools.Update(pool);
        db.Wallets.Update(wallet);

        // Log trade event
        await LogTradeEvent(new PoolTradeLog
        {
            TradeType = TradeType.Buy,
            Time = DateTime.UtcNow,
            Price = pool.PooledSecondaryCoin / pool.PooledPrimaryCoin,
            QuantityReceived = coinsReceived,
            CoinReceived = pool.PrimaryCoin,
            Pool = pool,
            Wallet = wallet
        });

        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    ///     Processes a sell order into the specified pool.
    /// </summary>
    /// <param name="accountGuid">The <see cref="Guid" /> identifying the account.</param>
    /// <param name="poolGuid">The <see cref="Guid" /> identifying the pool.</param>
    /// <param name="quantitySold">The quantity of base asset to be sold. </param>
    /// <returns>
    ///     A boolean indication trade success or failure.
    /// </returns>
    public async Task<bool> SellCoins(Guid accountGuid, Guid poolGuid, decimal quantitySold)
    {
        var pool = await GetPoolByGuid(poolGuid);
        if (pool is null) return false;
        // Get users wallet
        var wallet = await _walletManager.Get(accountGuid);

        // Return false if wallet doesnt have required coins
        if (!wallet.CheckBalance(pool.PrimaryCoin, quantitySold))
            return false;

        var feeAmount = quantitySold * FeePercentage;
        //TODO 
        //TODO Send fee amount to liquidity providers
        //TODO 

        var coinsReceived = CalculateReceivedOnSell(pool, quantitySold);

        // Ensure pool has coins
        if (coinsReceived <= 0) return false;
        if (coinsReceived >= pool.PooledSecondaryCoin) return false;

        // Remove the sold coins
        wallet.WithdrawCoin(pool.PrimaryCoin, quantitySold);

        // Add the purchased coins
        wallet.DepositCoin(pool.SecondaryCoin, coinsReceived);

        //Update liquidity pool
        pool.PooledPrimaryCoin += quantitySold - feeAmount;
        pool.PooledSecondaryCoin -= coinsReceived;

        db.Pools.Update(pool);
        db.Wallets.Update(wallet);

        // Log trade event
        // Log trade event
        await LogTradeEvent(new PoolTradeLog
        {
            TradeType = TradeType.Sell,
            Time = DateTime.UtcNow,
            Price = pool.PooledSecondaryCoin / pool.PooledPrimaryCoin,
            QuantityReceived = coinsReceived,
            CoinReceived = pool.SecondaryCoin,
            Pool = pool,
            Wallet = wallet
        });

        await db.SaveChangesAsync();

        return true;
    }

    private async Task LogTradeEvent(PoolTradeLog log)
    {
        await db.PoolTradeLogs.AddAsync(log);
    }

    private static decimal CalculateReceivedOnBuy(Pool pool, decimal quantitySold)
    {
        var feeAmount = quantitySold * FeePercentage;
        var dBeta = quantitySold - feeAmount;
        var k = pool.PooledPrimaryCoin * pool.PooledSecondaryCoin;
        var rAlpha = pool.PooledPrimaryCoin;
        var rBeta = pool.PooledSecondaryCoin;
        var dAlpha = k / (rBeta + dBeta);
        return rAlpha - dAlpha;
    }

    private static decimal CalculateReceivedOnSell(Pool pool, decimal quantitySold)
    {
        var feeAmount = quantitySold * FeePercentage;
        var dAlpha = quantitySold - feeAmount;
        var k = pool.PooledPrimaryCoin * pool.PooledSecondaryCoin;
        var rAlpha = pool.PooledPrimaryCoin;
        var rBeta = pool.PooledSecondaryCoin;
        var dBeta = k / (rAlpha - dAlpha);
        return dBeta - rBeta;
    }
}