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
    
    public required decimal ClaimAmount { get; set; }
    
    public virtual required Wallet Wallet { get; set; }
    
    public virtual required Coin Coin { get; set; }
}