namespace Server.Web.Hubs.Routes.Trade.Requests;

public class PoolOrderRequest
{
    public required Guid PoolGuid { get; set; }
    public required decimal Quantity { get; set; }
}