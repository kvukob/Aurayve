using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Web.Models;

namespace Server.Core.Coins;

[ApiController]
[Route("api/{controller}")]
[Produces("application/json")]
public class CoinController(AppDbContext db) : Controller
{
    [HttpGet]
    [Route("{symbol}")]
    public async Task<IActionResult> GetDetails(string symbol)
    {
        var coin = await db.Coins.FirstOrDefaultAsync(c => c.Symbol == symbol);
        return Ok(new ApiResponse()
        {
            Success = coin is not null,
            Data = new {Coin = coin}
        });
    }
}