namespace Server.Core.Accounts.Requests;

public class VerifyRegistrationRequest
{
    public required string Email { get; set; }
    public required string VerificationCode { get; set; }
}