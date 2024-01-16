using System.Text.Json.Serialization;

namespace Server.Web.Models;

public class LocationResponse
{
    [JsonPropertyName("country")] public string Country { get; set; } = null!;

    [JsonPropertyName("regionName")] public string Region { get; set; } = null!;
}