using CryBot.Contracts;
using CryBot.Core.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Orleans;

using System.Threading.Tasks;

namespace CryBot.Web.Controllers
{
    [Route("api/traders")]
    public class TradersController : Controller
    {
        private readonly IClusterClient _clusterClient;

        public TradersController(IOptions<EnvironmentConfig> options, IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrader([FromQuery] string market)
        {
            var trader = _clusterClient.GetGrain<ITraderGrain>(market);
            var traderResponse = new CryptoResponse<TraderState>(await trader.GetTraderData());
            return Ok(new { trader = traderResponse.Content, isSuccessful = true });
        }
    }
}
