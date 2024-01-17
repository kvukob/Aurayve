using System.ComponentModel.DataAnnotations;
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
    
    public bool LoginSuccessful { get; set; }
    
    [MaxLength(45)] 
    public string? IPAddress { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(100)]
    public string? Region { get; set; } 
    
    [MaxLength(255)] 
    public string? UserAgent { get; set; }
    
    [MaxLength(225)]
    public string? Details { get; set; }

    [JsonIgnore] public virtual required Account Account { get; set; }
}