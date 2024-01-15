using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Coins;

namespace Server.Core.Trading;

public class Pool
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    public int Id { get; set; }

    public Guid Guid { get; set; } = Guid.NewGuid();
    public decimal PooledPrimaryCoin { get; set; }
    public decimal PooledSecondaryCoin { get; set; }
    public virtual Coin PrimaryCoin { get; set; } = null!;
    public virtual Coin SecondaryCoin { get; set; } = null!;
    public virtual Coin LiquidityCoin { get; set; } = null!;
}