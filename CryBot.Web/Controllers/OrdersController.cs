using CryBot.Core.Models;
using CryBot.Core.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Threading.Tasks;
using System.Collections.Generic;

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
        public async Task<IActionResult> GetOrders([FromQuery]OrderType orderType)
        {
            CryptoResponse<List<CryptoOrder>> ordersResponse = null;
            switch (orderType)
            {
                case OrderType.OpenOrders:
                    ordersResponse = await _cryptoApi.GetOpenOrdersAsync();
                    break;
                case OrderType.CompletedOrders:
                    ordersResponse = await _cryptoApi.GetCompletedOrdersAsync();
                    break;
            }

            if (ordersResponse != null && ordersResponse.IsSuccessful)
                return Ok(new { orders = ordersResponse.Content, isSuccessful = true});
            return BadRequest(new { ordersResponse.ErrorMessage });
        }
    }
}
