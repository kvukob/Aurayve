namespace Server.Web.Models;

public class ClientInformation
{
    public string IPAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Region { get; set; } = null!;
}