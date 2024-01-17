using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Server.Core.Trading;
using Server.Database;
using Server.Web.Hubs.Routes.Trade.Requests;
using Server.Web.Models;

namespace Server.Web.Hubs.Routes.Trade;

public class TradeRoute(AppDbContext db)
{
    private readonly TradingManager _tradingManager = new(db);

    public async Task<HubResponse> Handle(string command, string request, Guid accountGuid)
    {
        return command switch
        {
            "get-pools" => await GetPools(),
            "add-liquidity" => await AddLiquidity(request, accountGuid),
            "buy-order" => await ProcessBuyOrder(request, accountGuid),
            "sell-order" => await ProcessSellOrder(request, accountGuid),
            "get-recent-trades" => await GetRecentTrades(request),
            "get-chart-data" => await GetChartData(request),
            _ => new HubResponse()
        };
    }


    private async Task<HubResponse> GetPools()
    {
        return new HubResponse
        {
            Success = true,
            Message = null,
            Data = new
            {
                PoolList = await _tradingManager.GetPools()
            }
        };
    }

    private async Task<HubResponse> AddLiquidity(string request, Guid accountGuid)
    {
        var req = JsonSerializer.Deserialize<AddLiquidityRequest>(request, new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        });
        if (req is null) return new HubResponse { Success = false };

        var success = await _tradingManager.AddLiquidity(accountGuid, req.PoolGuid, req.PrimaryCoinQuantity,
            req.SecondaryCoinQuantity);
        return new HubResponse
        {
            Success = success
        };
    }

    private async Task<HubResponse> ProcessBuyOrder(string request, Guid accountGuid)
    {
        var req = JsonSerializer.Deserialize<PoolOrderRequest>(request, new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        });
        if (req is null) return new HubResponse { Success = false };

        var success = await _tradingManager.BuyCoins(accountGuid, req.PoolGuid, req.Quantity);

        return new HubResponse
        {
            Success = success
        };
    }

    private async Task<HubResponse> ProcessSellOrder(string request, Guid accountGuid)
    {
        var req = JsonSerializer.Deserialize<PoolOrderRequest>(request, new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        });
        if (req is null) return new HubResponse { Success = false };

        var success = await _tradingManager.SellCoins(accountGuid, req.PoolGuid, req.Quantity);

        return new HubResponse
        {
            Success = success
        };
    }

    [AllowAnonymous]
    private async Task<HubResponse> GetRecentTrades(string request)
    {
        var req = JsonSerializer.Deserialize<GetRecentTradesRequest>(request);
        if (req is null) return new HubResponse { Success = false };
        var tradeList = await _tradingManager.GetPoolTrades(req.PoolGuid);

        return new HubResponse
        {
            Success = true,
            Data = new
            {
                RecentTrades = tradeList
            }
        };
    }

    [AllowAnonymous]
    private async Task<HubResponse> GetChartData(string request)
    {
        var req = JsonSerializer.Deserialize<ChartDataRequest>(request);
        if (req is null) return new HubResponse { Success = false };
        var chartData = await _tradingManager.GetChartData(req.PoolGuid);
        return new HubResponse
        {
            Success = true,
            Data = new
            {
                ChartData = chartData
            }
        };
    }
}