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

    [JsonIgnore] public virtual  Account Account { get; set; }

    public WalletBalance? GetBalance(Coin coin)
    {
        return Balances.FirstOrDefault(b => b.Coin == coin);
    }

    public void DepositCoin(Coin coin, decimal quantity)
    {
        var balance = GetBalance(coin);

        if (balance is null)
        {
            balance = new WalletBalance
            {
                Coin = coin,
                Quantity = quantity,
                Wallet = this
            };
            Balances.Add(balance);
        }
        else
        {
            balance.Quantity += quantity;
        }
    }
    public bool WithdrawCoin(Coin coin, decimal quantity)
    {
        var balance = GetBalance(coin);
        if (balance is null)
            return false;
        balance.Quantity -= quantity;
        return true;
    }

    public bool CheckBalance(Coin coinToCheck, decimal quantityToCheck)
    {
        var balance = GetBalance(coinToCheck);
        if (balance is null) return false;  // Coin not owned
        return quantityToCheck <= balance.Quantity;
    }
}