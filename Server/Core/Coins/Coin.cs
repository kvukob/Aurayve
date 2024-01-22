using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Server.Core.Coins;

public class Coin
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    public int Id { get; set; }
    [MaxLength(25)] 
    public required string Name { get; set; }
    [MaxLength(5)] 
    public required string Symbol { get; set; }
    public decimal CirculatingSupply { get; set; }
    public decimal MaxSupply { get; set; }

    public bool AddSupply(decimal amountToAdd)
    {
        if (amountToAdd + CirculatingSupply > MaxSupply)
            return false;
        if (amountToAdd <= 0)
            return false;
        CirculatingSupply += amountToAdd;
        return true;
    }
}