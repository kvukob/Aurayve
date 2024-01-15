using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Coins;
using Server.Core.Trading;
using Server.Core.Wallets;

namespace Server.Logging;

public class PoolTradeLog
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required TradeType TradeType { get; set; }
    public required DateTime Time { get; set; }
    public required decimal Price { get; set; }
    public required decimal QuantityReceived { get; set; }
    public virtual required Coin CoinReceived { get; set; }
    public virtual required Pool Pool { get; set; }
    public virtual required Wallet Wallet { get; set; }
}