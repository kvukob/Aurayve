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
    public required decimal PooledPrimaryCoin { get; set; }
    public required decimal PooledSecondaryCoin { get; set; }
    public virtual required Coin PrimaryCoin { get; set; }
    public virtual required Coin SecondaryCoin { get; set; }
    public virtual required Coin LiquidityCoin { get; set; }
}