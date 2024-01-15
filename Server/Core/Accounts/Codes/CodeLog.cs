using System.ComponentModel.DataAnnotations;

namespace Server.Core.Accounts.Codes;

public class CodeLog
{
    [Key] [MaxLength(6)] public string Code { get; set; } = null!;

    [MaxLength(255)] public string Email { get; set; } = null!;

    public DateTime ExpirationDate { get; set; }

    public CodeType Type { get; set; }
}