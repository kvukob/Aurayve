using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Coins;

namespace Server.Core.Wallets;

public class WalletBalance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    public int Id { get; set; }
    public decimal Quantity { get; set; }
    public virtual required Coin Coin { get; set; }
    [JsonIgnore] 
    public virtual  Wallet Wallet { get; set; } 
}   