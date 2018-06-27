using CryBot.Core.Models;
using CryBot.Core.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Threading.Tasks;

namespace CryBot.Web.Controllers
{
    [Route("api/orders")]
    public class OrdersController : Controller
    {
        private readonly ICryptoApi _cryptoApi;

        public OrdersController(ICryptoApi cryptoApi, IOptions<EnvironmentConfig> options)
        {
            _cryptoApi = cryptoApi;
            _cryptoApi.Initialize(options.Value.BittrexApiKey, options.Value.BittrexApiSecret);
        }

        [HttpGet]
        public async Task<IActionResult> GetOpenOrders()
        {
            var orders = await _cryptoApi.GetOpenOrdersAsync();
            return Ok(orders);
        }
    }
}
