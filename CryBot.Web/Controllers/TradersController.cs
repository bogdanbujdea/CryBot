using CryBot.Core.Trader;
using CryBot.Core.Storage;
using CryBot.Core.Exchange;
using CryBot.Core.Infrastructure;
using CryBot.Core.Exchange.Models;

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
        private readonly ITradersManager _tradersManager;

        public TradersController(IOptions<EnvironmentConfig> options, IClusterClient clusterClient, ICryptoApi cryptoApi, ITradersManager tradersManager)
        {
            _clusterClient = clusterClient;
            _tradersManager = tradersManager;
        }

        public async Task<IActionResult> GetTrader([FromQuery] string market)
        {
            var trader = _clusterClient.GetGrain<ITraderGrain>(market);
            var traderResponse = new CryptoResponse<TraderState>(await trader.GetTraderData());
            return Ok(new { trader = traderResponse.Content, isSuccessful = true });
        }

        [HttpGet("create")]
        public async Task<IActionResult> CreateTrader([FromQuery] string market)
        {
            await _tradersManager.CreateTraderAsync(market);
            return Ok();
        }

        [HttpGet("chart")]
        public async Task<IActionResult> GetChartForTrader([FromQuery] string market)
        {
            var chart = await _tradersManager.GetChartAsync(market);
            return Ok(chart);
        }

        [HttpGet]
        public async Task<IActionResult> GetTraders([FromQuery] string market)
        {
            if (string.IsNullOrWhiteSpace(market))
            {
                var traders = await _tradersManager.GetAllTraders();
                return Ok(new { traders, isSuccessful = true });
            }

            return await GetTrader(market);
        }
    }
}
