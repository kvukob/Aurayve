using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Server.Core.Accounts;

public class Account
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [JsonIgnore] 
    public Guid Guid { get; private set; } = Guid.NewGuid();
    [JsonIgnore, MaxLength(100)] 
    public string Email { get; set; } = null!;
    [MaxLength(35)] 
    public string? Username { get; set; }
    [JsonIgnore, MaxLength(84)] 
    public string HashedPassword { get; set; } = null!;
    [JsonIgnore] 
    public DateTime DateRegistered { get; private set; } = DateTime.UtcNow;

    /*
     *  Masks an account email address
     *  Turns testname@host.com -> te*****e@host.com
     */
    public static string MaskEmail(string email)
    {
        return Regex.Replace(email, @"(?<=^[^@]{2,})[^@](?=[^@]{1,}@)", "*");
    }
}