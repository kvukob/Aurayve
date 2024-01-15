namespace Server.Core.Accounts.Requests;

public class ChangeEmailRequest
{
    public string NewEmail { get; set; } = null!;
    public string NewEmailCode { get; set; } = null!;
    public string CurrentEmailCode { get; set; } = null!;
}