using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Coins;
using Server.Core.Wallets;

namespace Server.Logging;

public class FaucetLog
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime ClaimTime { get; set; }
    public double ClaimAmount { get; set; }
    public virtual Wallet Wallet { get; set; } = null!;
    public virtual Coin Coin { get; set; } = null!;
}