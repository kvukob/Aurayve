using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Server.Core.Coins;

public class Coin
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    public int Id { get; set; }

    [MaxLength(25)] public string Name { get; set; } = null!;
    [MaxLength(5)] public string Symbol { get; set; } = null!;
}