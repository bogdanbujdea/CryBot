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
            var ordersResponse = await _cryptoApi.GetOpenOrdersAsync();
            if (ordersResponse.IsSuccessful)
                return Ok(new { orders = ordersResponse.Content, isSuccessful = true});
            return BadRequest(new { ordersResponse.ErrorMessage });
        }
    }
}
