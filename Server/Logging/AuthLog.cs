using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Server.Core.Accounts;

namespace Server.Logging;

public class AuthLog
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IPAddress { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public bool LoginSuccessful { get; set; }
    public string Details { get; set; } = string.Empty;

    [JsonIgnore] public virtual Account Account { get; set; } = null!;
}