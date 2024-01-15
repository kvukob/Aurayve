namespace Server.Web.Hubs.Routes.Trade.Requests;

public class PoolOrderRequest
{
    public Guid PoolGuid { get; set; }
    public decimal Quantity { get; set; }
}