namespace Server.Core.Accounts.Requests;

public class ChangeEmailRequest
{
    public required string NewEmail { get; set; }
    public required string NewEmailCode { get; set; }
    public required string CurrentEmailCode { get; set; }
}