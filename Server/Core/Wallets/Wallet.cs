using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Accounts;
using Server.Core.Coins;

namespace Server.Core.Wallets;

public class Wallet
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    public int Id { get; set; }

    public virtual ICollection<WalletBalance> Balances { get; set; } = null!;

    [JsonIgnore] public virtual Account Account { get; set; } = null!;

    public WalletBalance? GetBalance(Coin coin)
    {
        return Balances.FirstOrDefault(b => b.Coin == coin);
    }
}