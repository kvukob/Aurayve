namespace Server.Web.Hubs.Routes.Trade.Requests;

public class AddLiquidityRequest
{
    public required Guid PoolGuid { get; set; }
    public required decimal PrimaryCoinQuantity { get; set; }
    public required decimal SecondaryCoinQuantity { get; set; }
}