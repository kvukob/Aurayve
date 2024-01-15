namespace Server.Core.Coins.Requests;

public class CreateCoinRequest
{
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    public required long TotalSupply { get; set; }
}