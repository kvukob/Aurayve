namespace Server.Web.Hubs.Routes.Trade.Requests;

public class AddLiquidityRequest
{
    public Guid PoolGuid { get; set; }
    public decimal PrimaryCoinQuantity { get; set; }
    public decimal SecondaryCoinQuantity { get; set; }
}