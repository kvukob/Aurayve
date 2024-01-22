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

    [JsonIgnore] public Guid Guid { get; private set; } = Guid.NewGuid();

    [JsonIgnore] [MaxLength(100)] public required string Email { get; set; }

    [MaxLength(12)] public string Username { get; set; } = string.Empty;

    [JsonIgnore] [MaxLength(84)] public required string HashedPassword { get; set; }

    [JsonIgnore] public DateTime DateRegistered { get; private set; } = DateTime.UtcNow;

    /*
     *  Masks an account email address
     *  Turns testname@host.com -> te*****e@host.com
     */
    public static string MaskEmail(string email)
    {
        return Regex.Replace(email, @"(?<=^[^@]{2,})[^@](?=[^@]{1,}@)", "*");
    }
}