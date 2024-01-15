using Server.Core.Accounts.Codes;

namespace Server.Core.Accounts.Requests;

public class VerificationCodeRequest
{
    public string? Email { get; set; }
    public CodeType Type { get; set; }
}