namespace Server.Core.Accounts.Requests;

public class FinalizeRegistrationRequest
{
    public required string Email { get; set; }
    public required string VerificationCode { get; set; }
    public required string Password { get; set; }
}