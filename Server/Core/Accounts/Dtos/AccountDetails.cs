namespace Server.Core.Accounts.Models;

public class AccountDetails
{
    public string MaskedEmail { get; set; } = string.Empty;
    public DateTime DateRegistered { get; set; }
    public DateTime LastLogin { get; set; }
}