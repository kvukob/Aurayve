using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Coins;

namespace Server.Core.Wallets;

public class WalletBalance
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    public int Id { get; set; }

    public virtual Coin Coin { get; set; } = null!;
    public decimal Quantity { get; set; }

    [JsonIgnore] public virtual Wallet Wallet { get; set; } = null!;
}