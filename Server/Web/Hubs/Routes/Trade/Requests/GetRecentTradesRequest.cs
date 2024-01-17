namespace Server.Web.Hubs.Routes.Trade.Requests;

public class GetRecentTradesRequest
{
    public required Guid PoolGuid { get; set; }
}